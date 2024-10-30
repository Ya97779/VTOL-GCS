using Accord.Video.FFMPEG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HMI
{

    public class ScreenRecorder
    {
        public  Thread recordingThread;
        public void StartRecording(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {

                Form1.videowriter.Open(path, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, 25, VideoCodec.MPEG4,5000000);
                recordingThread = new Thread(RecordScreen);
                recordingThread.Start();


            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                MessageBox.Show("屏幕录制开启失败: " + err.Message, "提示！");
            }
        }

        public void StopRecording()
        {
            try
            {
                recordingThread.Join();
                Form1.videowriter.Close();


            }
            catch (Exception err)
            {
                MessageBox.Show("屏幕录制停止失败: " + err.Message, "提示！");
                Console.WriteLine(err.ToString());
            }
        }

        private void RecordScreen()
        {
            using (Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    //while (Form1.isRecording)
                    //{
                    //    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "添加图像前");
                    //    g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                    //    Form1.videowriter.WriteVideoFrame(bmp);
                    //    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "添加图像后，即Threadsleep开始");
                    //    // 原本用于控制帧率，经测试：由于图像复制写入时间约为40ms，正好为25帧，因此不需要再等待40ms；
                    //    // 否则就会导致每次写入的时间为80ms/帧，但是视频播放40ms/帧，从而使得录制得到的视频像是快进了一样。
                    //    Thread.Sleep(1000 / 25);
                    //    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "Threadsleep结束");
                    //}

                    while (Form1.isRecording)
                    {
                        Stopwatch stopwatch = Stopwatch.StartNew();//开始计时
                        
                        // 执行屏幕捕获和视频帧写入
                        g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                        Form1.videowriter.WriteVideoFrame(bmp);
                        
                        TimeSpan timeElapsed = stopwatch.Elapsed;//stopwatch.Elapsed 返回 Stopwatch 启动以来经过的时间。
                        Console.WriteLine("写入时长是"+timeElapsed.TotalMilliseconds);
                        TimeSpan targetInterval = TimeSpan.FromTicks(40 * 10000); // 时间间隔40毫秒，即25帧


                        // 计算下一帧应该开始的时间
                        TimeSpan nextFrameTime = timeElapsed + targetInterval;

                        // 等待直到下一帧的开始时间
                        if (nextFrameTime > stopwatch.Elapsed)
                        {
                            // 计算需要等待的时间
                            int sleepTime = (int)(targetInterval - timeElapsed).TotalMilliseconds;
                            if (sleepTime > 0)
                            {
                                //Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "Threadsleep开始");
                                Thread.Sleep(sleepTime);
                                //console.writeline(datetime.now.tostring("yyyy-mm-dd hh:mm:ss.fff") + "threadsleep结束");

                            }
                        }


                        stopwatch.Restart(); // 重置计时器
                    }
                }
            }
        }


    }
}
    

