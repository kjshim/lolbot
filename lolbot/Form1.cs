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
    public class Minion
    {
        public Point p;
        List<TimedHP> hpQ = new List<TimedHP>();

        private int FRESH_TIME = 2500;

        private struct TimedHP
        {
            public int timeStamp; // in seconds
            public float hp;
        }

        public void Update(Point p_new, int timestamp, float hp)
        {
            p = p_new;
            // remove old ones
            hpQ = hpQ.FindAll(delegate(TimedHP thp) { return timestamp - thp.timeStamp <= FRESH_TIME; });

            TimedHP newthp = new TimedHP();
            newthp.hp = hp;
            newthp.timeStamp = timestamp;
            hpQ.Add(newthp);
            
        }

        public float GetPredictionAfter(int seconds)
        {
            TimedHP recent = hpQ.Last();
            TimedHP oldest = hpQ.First();

            // calc simple deriv
            double dx = recent.timeStamp - oldest.timeStamp;

            if(dx <= 0.0){ return recent.hp; }
            double dy = recent.hp - oldest.hp;
            double result = recent.hp + dy / dx * seconds;
            return (float)result;
        }

        public override string ToString()
        {
            TimedHP recent = hpQ.Last();
            StringBuilder builder = new StringBuilder();
            builder.Append("(" + p.ToString() + ") : [");
            foreach(TimedHP thp in hpQ){
                builder.Append("( " + (thp.timeStamp - recent.timeStamp).ToString() + "," + thp.hp + "), ");
            }
            builder.Append("]");
            return builder.ToString();

        }
    }


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


        int TIME_PER_ATTACK = 500;
        Point pHome = new Point(936, 780);
        IntPtr hWnd = IntPtr.Zero;
        RECT wRect = new RECT();
        Boolean running = false;
        ILog logger = log4net.LogManager.GetLogger("LolBot");
        
        public Form1()
        {
            InitializeComponent();

            
        }

        KeyEventHandler keyCallback;
    
        private void InitBot() {
            logger.Info("Initializing bot...");

            if (!getLoLWindow())
            {
                logger.Error("Can't find LoL Window");
            }
          
            // add keyboard hook
            logger.Info("Adding Keyboard hooks..");
            gkh.HookedKeys.Add(Keys.F1);
            gkh.HookedKeys.Add(Keys.F2);
            gkh.HookedKeys.Add(Keys.F3);
            keyCallback = new KeyEventHandler(onHookKeyDown);
            gkh.KeyDown += keyCallback;          
        }

        void onHookKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                logger.Debug("Catched F1. Starting monitoring");
                Start();
                e.Handled = true;
            }
            if (e.KeyCode == Keys.F2)
            {
                logger.Debug("Catched F2. End monitoring");
                End();
                e.Handled = true;
            }
            if (e.KeyCode == Keys.F3)
            {
                logger.Debug("Catched F3. Setting Home");
                SetCursorAsHome();
                e.Handled = true;
            }
        }
        

        private void FinishBot()
        {
            logger.Info("Finishing bot...");
        }
        
        private bool getLoLWindow()
        {
            hWnd = WndSearcher.SearchForWindow("", "League of Legends");
            if (hWnd != IntPtr.Zero)
            {
                
                GetWindowRect(hWnd, ref wRect);
                //SetForegroundWindow(hWnd);
                logger.Debug("Located LOL Window : (" + wRect.Left.ToString() + "," + wRect.Top.ToString() + ")");
                return true;
            }
            return false;
        }

        private Bitmap CaptureGameRegion()
        {
            Size size = new Size(wRect.Right - wRect.Left, wRect.Bottom - wRect.Top);
            Bitmap bmp = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);
            g.CopyFromScreen(wRect.Left, wRect.Top, 0, 0, size);
            return bmp;
        }

        // managed minions
        List<Minion> enemy_minions = new List<Minion>();
        public class MinionCandidate
        {
            public Point p;
            public float hp;
        }


        private void processImage(Bitmap bmp, Boolean doAction)
        {
            
            int timestamp = Environment.TickCount & Int32.MaxValue;
            logger.Debug("Start process image : " + DateTime.Now.ToString());

            // Image State
            List<MinionCandidate> l_MinionCandidates = new List<MinionCandidate>();
            List<MinionCandidate> our_MinionCandidates = new List<MinionCandidate>();
            
            
            BitmapData bmd = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                  System.Drawing.Imaging.ImageLockMode.ReadWrite,
                                  bmp.PixelFormat);
            Point center = new Point( bmp.Width / 2,bmp.Height / 2);
            
            
            // find MinionCandidates
            unsafe
            {
                int BLUE_OFFSET = 0;
                int GREEN_OFFSET = 1;
                int RED_OFFSET = 2;
                int ALPHA_OFFSET = 3;

                int healthbar_width_half = 32;
                int MinionCandidate_height_half = 22;
            
                int PixelSize=4;

                // find MinionCandidates
                for (int y = 1; y < bmd.Height; y++)
                {
                    // blue, green, red, alpha
                    byte* prevrow = (byte*)bmd.Scan0 + ((y - 1) * bmd.Stride);
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride);

                    for (int x = 1; x < bmd.Width; x++)
                    {
                        // red
                        byte p1 = prevrow[(x-1) * PixelSize + RED_OFFSET];
                        byte p2 = prevrow[(x) * PixelSize + RED_OFFSET];
                        byte p3 = row[(x - 1) * PixelSize + RED_OFFSET];
                        byte p4 = row[(x) * PixelSize + RED_OFFSET];
                        if (p1 == 0 && p2 == 0 && p3 == 0 && p4 >170)
                        {
                            int x_org = x;
                            int hp = 0;
                            while (row[x * PixelSize + RED_OFFSET] >170)
                            {
                                x++;
                            }
                            hp = x - x_org;
                            while (prevrow[x * PixelSize + RED_OFFSET] == 0)
                            
                            {
                                x++;
                            }
                            int all = x - x_org;
                            if (all < 30) { continue; }
                            if (all > 70) {
                                logger.Debug("Found Champ maybe? All length:" + all.ToString() + " x:" + (x_org + 60).ToString() + " y:" + (y + 70).ToString());
                                continue; 
                            }
                            float hp_perc = (float)hp / (float)all;
                            MinionCandidate m = new MinionCandidate();
                            m.p.X = x_org + healthbar_width_half;
                            m.p.Y = y + MinionCandidate_height_half;
                            m.hp = hp_perc;
                            l_MinionCandidates.Add(m);

                            string msg = String.Format("Enemy x:{0} y:{1} hp:{2} all:{3} prec:{4}", m.p.X, m.p.Y, hp, all, hp_perc);
                            logger.Debug(msg);
                        }

                        // green
                        p1 = prevrow[(x - 1) * PixelSize + GREEN_OFFSET];
                        p2 = prevrow[(x) * PixelSize + GREEN_OFFSET];
                        p3 = row[(x - 1) * PixelSize + GREEN_OFFSET];
                        p4 = row[(x) * PixelSize + GREEN_OFFSET];

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
                            MinionCandidate m = new MinionCandidate();
                            m.p.X = x_org + 32;
                            m.p.Y = y + 22;
                            m.hp = hp_perc;
                            our_MinionCandidates.Add(m);

                            string msg = String.Format("Our x:{0} y:{1} hp:{2} all:{3} prec:{4}", m.p.X, m.p.Y, hp, all, hp_perc);
                            logger.Debug(msg);
                        }
                        
                        //byte b = row[x * PixelSize];   //Blue  0-255
                        //byte gr = row[x * PixelSize + 1]; //Green 0-255
                        //byte r = row[x * PixelSize + 2]; //Red   0-255
                        //byte al = row[x * PixelSize + 3]; //Alpha 0-255
                    }
                }
            }
            bmp.UnlockBits(bmd);


            // Match MinionCandidates with Minions
            List<Minion> new_enemy_list = new List<Minion>();
            while (l_MinionCandidates.Count > 0)
            {
                if(enemy_minions.Count <= 0){
                    // no more managed known minions
                    // put all remaining minion candidates into new list
                    foreach( MinionCandidate mc in l_MinionCandidates ){
                        Minion new_minion = new Minion();
                        new_minion.Update( mc.p, timestamp, mc.hp );
                        new_enemy_list.Add(new_minion);
                        logger.Debug("[Minion] New     : " + new_minion.ToString() + " Expected HP:" + new_minion.GetPredictionAfter(TIME_PER_ATTACK).ToString());
                    }
                    break;
                }

                // find closest pair
                Minion closest_m = null;
                MinionCandidate closest_mc = null;
                double min_dist = double.MaxValue;
                foreach (MinionCandidate mc in l_MinionCandidates)
                {
                    foreach (Minion m in enemy_minions)
                    {
                        double dist = DistanceTo(mc.p, m.p);
                        if (dist < min_dist)
                        {
                            min_dist = dist;
                            closest_m = m;
                            closest_mc = mc;
                        }
                    }
                }
                closest_m.Update(closest_mc.p, timestamp, closest_mc.hp);
                new_enemy_list.Add(closest_m);
                logger.Debug("[Minion] Matched : " + closest_m.ToString() + " Expected HP:" + closest_m.GetPredictionAfter(TIME_PER_ATTACK));

                l_MinionCandidates.Remove(closest_mc);
                enemy_minions.Remove(closest_m);
            }
            enemy_minions = new_enemy_list;
            
            // decide actions
            if (doAction)
            {
                double thres = 0.09 * Math.Max((our_MinionCandidates.Count / (enemy_minions.Count+1))*0.8, 1.0);

                double mindist_enemy = double.MaxValue / 2 ;
                double mindist_our = double.MaxValue;
                foreach (Minion m in enemy_minions)
                {
                    if (DistanceTo(m.p, center) < mindist_enemy)
                    {
                        mindist_enemy = DistanceTo(m.p, center);
                    }
                }
                foreach (MinionCandidate m in our_MinionCandidates)
                {
                    if (DistanceTo(m.p, center) < mindist_our)
                    {
                        mindist_our = DistanceTo(m.p, center);
                    }
                }

                logger.Debug("Min dist our:" + mindist_our.ToString() + " Min dist enemy: " + mindist_enemy.ToString());
                if(!(mindist_our + 50 < mindist_enemy))
                {
                    rClick(pHome);
                    logger.Debug("Going Home. Min dist our:" + mindist_our.ToString() + " Min dist enemy: " + mindist_enemy.ToString());
                    return;
                }

                Boolean found = false;
                Point target;
                target = Point.Empty;

                // lost shot first
                foreach (Minion m in enemy_minions)
                {
                    if (m.GetPredictionAfter(TIME_PER_ATTACK) < thres)
                    {
                        if (found && DistanceTo(m.p, center) < DistanceTo(target, center))
                        {
                            target = m.p;
                        }
                        else
                        {
                            found = true;
                            target = m.p;
                        }
                    }
                }

                if (found)
                {
                    logger.Debug("Killing Minion");
                    rClick(target);
                    Thread.Sleep(TIME_PER_ATTACK / 2);
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
                    //// need to close our minion
                    //// if 
                    //foreach (MinionCandidate m in our_MinionCandidates)
                    //{
                    //    if (DistanceTo(m.p, center) < mindist)
                    //    {
                    //        mindist = DistanceTo(m.p, center);
                    //        closest = m.p;
                    //    }
                    //}

                    //if (400 < mindist)
                    //{
                    //    size = 1;
                    //    farest = closest;
                    //    maxdist = 99999.0;
                    //}

                    if (size == 0)
                    {
                        foreach (Minion m in enemy_minions)
                        {
                            double expHP = m.GetPredictionAfter(TIME_PER_ATTACK);
                            if ( expHP < 0.5)
                            {
                                size += 1;

                                if (m.GetPredictionAfter(TIME_PER_ATTACK) < minhp)
                                {
                                    maxdist = DistanceTo(m.p, center);
                                    farest = m.p;
                                    minhp = expHP;
                                }
                            }
                        }
                    }

                    if (size == 0 && our_MinionCandidates.Count > 0)
                    {
                        // follow our minion
                        maxdist = 999.0;
                        farest = our_MinionCandidates[0].p;
                        size = 1;
                    }

                    if (size > 0)
                    {
                        Random rand = new Random();
                        double range = 170.0;
                        Point x = new Point();
                        if (maxdist < range)
                        {
                            //x.X = farest.X - center.X;
                            //x.Y = farest.Y - center.Y;
                            //if (x.X > 0) x.X = 30 + (rand.Next() % 40 - 20);
                            //if (x.X < 0) x.X = 800 + (rand.Next() % 40 - 20);
                            //if (x.Y > 0) x.Y = 50 + (rand.Next() % 40 - 20);
                            //if (x.Y < 0) x.Y = 600 + (rand.Next() % 40 - 20);
                            // logger.Debug("escape to center");
                            // rClick(x);

                            logger.Debug("Stay center");
                            rClick(center);

                        }
                        else if (maxdist > range)
                        {
                            // stay slightly above ( to avoid champ HP bar overlay )
                            x.X = farest.X - center.X;
                            x.Y = (farest.Y - 50) - center.Y;
                            if (x.X > 0) x.X = 800 + (rand.Next() % 40 - 20);
                            if (x.X < 0) x.X = 30 + (rand.Next() % 40 - 20);
                            if (x.Y > 0) x.Y = 600 + (rand.Next() % 40 - 20);
                            if (x.Y < 0) x.Y = 50 + (rand.Next() % 40 - 20);

                            logger.Debug("Move to center");
                            rClick(x);
                        }
                    }
                }
            }

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
            if (!getLoLWindow())
            {
                logger.Error("Can't find LoL Window");
            }
          
            timer1.Enabled = true;
        }
        private void End()
        {
            timer1.Enabled = false;
        }

        private void SetCursorAsHome()
        {
            pHome = new Point(Cursor.Position.X - wRect.Left, Cursor.Position.Y - wRect.Top);
            logger.Debug("Setting home as : " + pHome.ToString());
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
            Bitmap bmp = CaptureGameRegion();
            if (running) { return; }
            if (!GetActiveWindowTitle().StartsWith("League of ")) { return; }
            running = true;
            processImage(bmp, true);
            running = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitBot();
        }

        private void From1_Closed(object sender, FormClosedEventArgs e)
        {
            FinishBot();
        }

        private void chooseFile_Click(object sender, EventArgs e)
        {
            logger.Info("Capturing game screen to save.");
            Bitmap bmp = CaptureGameRegion();

            saveImgFileDialog1.InitialDirectory = "D:";
            saveImgFileDialog1.Filter = "BMP Images|*.bmp";
            DialogResult r = saveImgFileDialog1.ShowDialog();
            if (r == DialogResult.OK)
            {
                bmp.Save(saveImgFileDialog1.FileName);
                logger.Info("Capture saved as : " + saveImgFileDialog1.FileName);
            }
        }

        Bitmap loadedBitmap;

        private void button2_Click(object sender, EventArgs e)
        {
            openImgFileDialog1.Filter = "BMP Images|*.bmp";
            DialogResult r = openImgFileDialog1.ShowDialog();
            if (r == DialogResult.OK)
            {
                Bitmap bmp = new Bitmap(openImgFileDialog1.FileName);
                pCapture.SizeMode = PictureBoxSizeMode.StretchImage;
                for(int i = 0; i < bmp.Height; i ++){
                    for( int j = 0; j < bmp.Width; j++){
                        Color p = bmp.GetPixel(j, i);
                        
                        bmp.SetPixel(j, i, Color.FromArgb( p.R, p.R, p.R));
                    }
                }
                pCapture.Image = bmp;
                loadedBitmap = bmp;
            }     
        
            processImage(loadedBitmap, false);
        }
    }
}
