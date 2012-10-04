using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing.Imaging;
using System.Collections;

using Utilities;

using log4net;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace lolbot
{
    public partial class Form1 : Form
    {
        //[System.Runtime.InteropServices.DllImport("user32.dll")]
        //static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
        //int info;
        //const int WM_LBUTTONDOWN = 0x02;
        //const int WM_LBUTTONUP = 0x04;
        //const int WM_RBUTTONUP = 0x04;
        //const int WM_RBUTTONDOWN = 0x205;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);


        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        //[StructLayout(LayoutKind.Sequential)]
        //const int WM_LBUTTONUP = 0x204;

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName,
            string lpWindowName);

        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        const UInt32 WM_SYSCOMMAND = 0x0112;
        const UInt32 SC_RESTORE    = 0xF120;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        globalKeyboardHook gkh = new globalKeyboardHook();
        
        private string GetActiveWindowTitle()
        {
            const int nChars = 256;
            IntPtr handle = IntPtr.Zero;
            StringBuilder Buff = new StringBuilder(nChars);
            handle = GetForegroundWindow();     

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return "";
        }

        

        public struct Minion
        {
            public Point p;
            public double dist;
            public float hp;
        }

        Point pHome = new Point(936, 780);
        IntPtr hWnd;
        RECT wRect;
        Boolean running = false;
        ILog logger = log4net.LogManager.GetLogger("LolBot");
        
        public Form1()
        {
            InitializeComponent();

            
        }
    
        private void InitBot() {
            logger.Info("Initializing bot...");

            hWnd = getLoLWindow();
            wRect = new RECT();
            GetWindowRect(hWnd, ref wRect);

            //            SendKeys.SendWait("111");
            SetForegroundWindow(hWnd);
            Thread.Sleep(100);

            // add keyboard hook
            logger.Info("Adding Keyboard hooks..");
            gkh.HookedKeys.Add(Keys.F1);
            gkh.HookedKeys.Add(Keys.F2);
            gkh.KeyDown += new KeyEventHandler(onHookKeyDown);

            gkh.hook();
        }

        private void FinishBot()
        {
            logger.Info("Finishing bot...");
            gkh.unhook();
            
        }
        void onHookKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                logger.Debug("Catched F1. Starting monitoring");
                Start();
            }
            if (e.KeyCode == Keys.F2)
            {
                logger.Debug("Catched F2. End monitoring");
                End();
            }
            e.Handled = true;
        }
        
        private IntPtr getLoLWindow()
        {
            foreach (Process proc in Process.GetProcesses())
            {
                if (proc.MainWindowTitle.StartsWith("League of "))
                {
                    IntPtr handle = proc.MainWindowHandle;
                    return handle;
                }
            }
            return IntPtr.Zero;
        }

        private void processImage()
        {
            if (running) { return; }
            if (!GetActiveWindowTitle().StartsWith("League of ")) { return; }
            running = true;

            listLog.Items.Clear();
            listLog.Items.Add("Start capturing : " + DateTime.Now.ToString());

            List<Minion> l_minions = new List<Minion>();
            List<Minion> our_minions = new List<Minion>();

            Bitmap bmp = new Bitmap(1024, 768, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(wRect.Left, wRect.Top, 0, 0, new Size(wRect.Right - wRect.Left, wRect.Bottom - wRect.Top));

            BitmapData bmd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                  System.Drawing.Imaging.ImageLockMode.ReadWrite,
                                  bmp.PixelFormat);
            Point center = new Point(518, 343);
            
            // find minions
            unsafe
            {
                int PixelSize=4;
                for (int y = 1; y < bmd.Height; y++)
                {
                    byte* prevrow = (byte*)bmd.Scan0 + ((y - 1) * bmd.Stride);
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride);

                    for (int x = 1; x < bmd.Width; x++)
                    {
                        byte p1 = prevrow[(x-1) * PixelSize + 2];
                        byte p2 = prevrow[(x) * PixelSize + 2];
                        byte p3 = row[(x - 1) * PixelSize + 2];
                        byte p4 = row[(x) * PixelSize + 2];
                        if (p1 == 0 && p2 == 0 && p3 == 0 && p4 == 255)
                        {
                            int x_org = x;
                            int hp = 0;
                            while (row[x * PixelSize + 2] == 255)
                            {
                                x++;
                            }
                            hp = x - x_org;
                            while (prevrow[x * PixelSize + 2] == 0)
                            
                            {
                                x++;
                            }
                            int all = x - x_org;
                            if (all < 30) { continue; }
                            if (all > 70) { continue; }
                            float hp_perc = (float)hp / (float)all;
                            Minion m = new Minion();
                            m.p.X = x_org + 32;
                            m.p.Y = y + 22;
                            m.hp = hp_perc;
                            m.dist = DistanceTo(m.p, center);
                            l_minions.Add(m);

                            string msg = String.Format("Enemy x:{0} y:{1} hp:{2} all:{3} prec:{4}", x_org, y, hp, all, hp_perc);
                            listLog.Items.Add(msg);
                        }


                        p1 = prevrow[(x - 1) * PixelSize + 1];
                        p2 = prevrow[(x) * PixelSize + 1];
                        p3 = row[(x - 1) * PixelSize + 1];
                        p4 = row[(x) * PixelSize + 1];

                        if (p1 == 0 && p2 == 0 && p3 == 0 && p4 > 200)
                        {
                            int x_org = x;
                            int hp = 0;
                            while (row[x * PixelSize + 1] > 200)
                            {
                                x++;
                            }
                            hp = x - x_org;
                            while (prevrow[x * PixelSize + 1] == 0)
                            {
                                x++;
                            }
                            int all = x - x_org;
                            if (all < 30) { continue; }
                            if (all > 70) { continue; }
                            float hp_perc = (float)hp / (float)all;
                            Minion m = new Minion();
                            m.p.X = x_org + 32;
                            m.p.Y = y + 22;
                            m.hp = hp_perc;
                            m.dist = DistanceTo(m.p, center);
                            our_minions.Add(m);

                            string msg = String.Format("Our x:{0} y:{1} hp:{2} all:{3} prec:{4}", x_org, y, hp, all, hp_perc);
                            listLog.Items.Add(msg);
                        }
                        
                        //byte b = row[x * PixelSize];   //Blue  0-255
                        //byte gr = row[x * PixelSize + 1]; //Green 0-255
                        //byte r = row[x * PixelSize + 2]; //Red   0-255
                        //byte al = row[x * PixelSize + 3]; //Alpha 0-255
                    }
                }
            }
            bmp.UnlockBits(bmd);

            // decide actions
            double advcnt = Math.Max(0.0, (double)(l_minions.Count) / our_minions.Count - 1.0);
            double thres = 0.15 + 0.1 * advcnt;

            
            Boolean found = false;
            Minion target;
            target.p = Point.Empty;


            double mindist_enemy = 99999.0;
            double mindist_our = 99999.0;
            foreach (Minion m in l_minions)
            {
                if (m.dist < mindist_enemy)
                {
                    mindist_enemy = m.dist;
                }
            }
            foreach (Minion m in our_minions)
            {
                if (m.dist < mindist_our)
                {
                    mindist_our = m.dist;
                }
            }
            
            if ( mindist_our >= mindist_enemy)
            {
                rClick(pHome);
                listLog.Items.Add("Going Home");
                running = false;
                return;
            }

            // lost shot first
            foreach(Minion m in l_minions){
                if (m.hp < thres) {
                    if (found && DistanceTo(m.p, center) < DistanceTo(target.p, center))
                    {
                        target = m;
                    }
                    else
                    {
                        found = true;
                        target = m;
                    }
                }
            }



            if (found)
            {
                listLog.Items.Add( "Killing Minion");
                rClick(target.p);
            }
            else
            {
                // Get center position of minions
                double maxdist = 0.0;
                double minhp = 99999.0;
                double mindist = 999999.0;
                Point farest = new Point();
                int size = 0;

                Point closest = new Point();
                // need to close our minion
                foreach (Minion m in our_minions)
                {
                    if (DistanceTo(m.p, center) < mindist)
                    {
                        mindist = DistanceTo(m.p, center);
                        closest = m.p;
                    }
                }

                if (mindist > 300 && mindist < 1000)
                {
                    size = 1;
                    farest = closest;
                    maxdist = 99999.0;
                }

                if (size == 0)
                {
                    foreach (Minion m in l_minions)
                    {
                        if (m.hp < 0.5)
                        {
                            size += 1;
                            
                            if (m.hp < minhp)
                            {
                                maxdist = DistanceTo(m.p, center);
                                farest = m.p;
                                minhp = m.hp;
                            }
                        }
                    }
                }

                if (size == 0 && our_minions.Count > 0)
                {
                    // follow our minion
                    maxdist = 999.0;
                    farest = our_minions[0].p;
                    size = 1;
                }

                if (size > 0)
                {   
                    Random rand = new Random();
                    double range = 150.0;
                    Point x = new Point();
                    if (maxdist < range )
                    {
                        x.X = farest.X - center.X;
                        x.Y = farest.Y - center.Y;
                        if (x.X > 0) x.X = 30 + (rand.Next() % 40 - 20);
                        if (x.X < 0) x.X = 800 +(rand.Next() % 40 - 20);
                        if (x.Y > 0) x.Y = 50 + (rand.Next() % 40 - 20);
                        if (x.Y < 0) x.Y = 600 + (rand.Next() % 40 - 20);

                        listLog.Items.Add("escape to center");
                        rClick(x);

                    }
                    else if (maxdist > range)
                    {
                        x.X = farest.X - center.X;
                        x.Y = farest.Y - center.Y;
                        if (x.X > 0) x.X = 800 + (rand.Next() % 40 - 20);
                        if (x.X < 0) x.X = 30 + (rand.Next() % 40 - 20);
                        if (x.Y > 0) x.Y = 600 + (rand.Next() % 40 - 20);
                        if (x.Y < 0) x.Y = 50 + (rand.Next() % 40 - 20);

                        listLog.Items.Add("Move to center");
                        rClick(x);
                    }
                }
            }


            running = false;
        }
        public double DistanceTo(Point point1, Point point2)
        {
            var a = (double)(point2.X - point1.X);
            var b = (double)(point2.Y - point1.Y);

            return Math.Sqrt(a * a + b * b);
        }
        public void rClick(Point p)
        {
            Point rp = new Point(wRect.Left + p.X, wRect.Top + p.Y);

            Cursor.Position = rp;
            mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
            mouse_event(MOUSEEVENTF_RIGHTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
            Thread.Sleep(100);
            

            //int x = 400;
            //int y = 400;
            //Cursor.Position = new Point(wRect.Left + x, wRect.Top+ y);
            ////mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
            //mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
            //mouse_event(MOUSEEVENTF_RIGHTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
            //Thread.Sleep(100);
            //mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
            //mouse_event(MOUSEEVENTF_RIGHTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
        }
        private void Start()
        {
            timer1.Enabled = true;
        }
        private void End()
        {
            timer1.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            timer1.Enabled = !timer1.Enabled;
            //int x = 400;
            //int y = 400;
            //Cursor.Position = new Point(wRect.Left + x, wRect.Top+ y);
            ////mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
            //mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
            //mouse_event(MOUSEEVENTF_RIGHTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
            //Thread.Sleep(100);
            //mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
            //mouse_event(MOUSEEVENTF_RIGHTUP, (uint)Cursor.Position.X, (uint)Cursor.Position.Y, 0, 0);
           
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            processImage();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitBot();
        }

        private void From1_Closed(object sender, FormClosedEventArgs e)
        {
            FinishBot();
        }
    }
}
