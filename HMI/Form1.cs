using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;    
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;
using Accord.Video.FFMPEG;
using static System.Net.Mime.MediaTypeNames;

namespace HMI
{
    public partial class Form1 : Form
    {
        
        private Queue<byte> bufferQueue = new Queue<byte>();//队列接收缓存区
        private const byte frame_head1 = 0x1A; // 定义帧头第1个字节
        private const byte frame_head2 = 0xCF; // 定义帧头第2个字节
        private const byte frame_tail1 = 0xFC; // 定义帧尾第1个字节
        private const byte frame_tail2 = 0x1D; // 定义帧尾第2个字节
        private bool isHeadReceive = false;   //帧头标志位
        private object queueLock = new object();//线程锁
        public static VideoFileWriter videowriter = new VideoFileWriter();
        public static bool isRecording = false;
        private string timestamp;
        private string csvFilePath;
        private string csvfileName;
        private string csvfullPath;
        private string screenfileName;
        private string screenfullPath;
        public DataSaver saver = new DataSaver();
        public ScreenRecorder recorder = new ScreenRecorder();
        public Error_Status Decoder = new Error_Status();
        public Form1()
        {
            InitializeComponent();
            csvFilePath = @"D:\HMIruns";
            csvfileName = $"data.csv";
            csvfullPath = Path.Combine(csvFilePath, csvfileName);

        }

        private void Form1_Load(object sender, EventArgs e)
        {

            Button_scancom_Click(this, EventArgs.Empty);
        }


        //串口扫描
        private void Button_scancom_Click(object sender, EventArgs e)
        {
            comboBox_com.Items.Clear();
            comboBox_com.Text = null; //清除原先串口数据
            string[] ports = System.IO.Ports.SerialPort.GetPortNames(); //获得可用的串口
           
            if (ports.Length ==0)
            {
                MessageBox.Show("没有检测到串口！", "Error");
                return;
            }
         
            for (int i = 0; i < ports.Length; i++)
                {
                 comboBox_com.Items.Add(ports[i]);
                }
                comboBox_com.SelectedIndex = comboBox_com.Items.Count > 0 ? 0 : -1;//如果里面有数据,显示第0个
            
        }
        //串口的开、关
        private void Button_opencom_Click(object sender, EventArgs e)
        {
          
         
                if (button_opencom.Text == "打开串口")
                {
                    try
                    {
                        serialPort1.PortName = comboBox_com.Text;//获取要打开的串口
                        serialPort1.BaudRate = int.Parse(comboBox_baudrate.Text);//获得波特率
                                                                                                                             
                        
                    serialPort1.Open();//打开串口
                    serialPort1.ReceivedBytesThreshold = 1; // 触发DataReceived 事件的字节数阈值，超过就会触发SerialPort1_DataReceived事件
                    comboBox_com.Enabled = false;
                    comboBox_baudrate.Enabled = false;
                    button_scancom.Enabled = false;
                    button_opencom.BackColor = Color.Green;
                    button_opencom.Text = "关闭串口";
                    saver.WriteCsvTitle(csvfullPath);

                }
                    catch (Exception err)
                    {
                        MessageBox.Show("打开失败" + err.ToString(), "提示！");
                    }
                }
                else
                {
                    //关闭串口
                    try
                    {
                      serialPort1.Close();//关闭串口
                      comboBox_com.Enabled = true;
                      comboBox_baudrate.Enabled = true;
                      button_scancom.Enabled = true;
                      button_opencom.BackColor = Color.White;
                      UIclear();
                      button_opencom.Text = "打开串口"; //按钮显示打开
                    }
                    catch (Exception err) 
                    {
                        MessageBox.Show("关闭失败" + err.ToString(), "提示！");
                    }
                    

                }
        
        }
        //读取数据并显示到textBox1中  SerialPort1_DataReceived为辅助线程，直接更新UI要跨线程 ，存在安全问题
        private void SerialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] dataTemp = new byte[serialPort1.BytesToRead];   //定义暂时数据接收字节数组
           

            serialPort1.Read(dataTemp, 0, dataTemp.Length);//把数据读取到dataTemp中，偏移为0，长度为当前串口可以读取的有效字节数
                                                           //   receiveBuffer.AddRange(dataTemp);  //将数据填加到缓存区列表中
            lock (queueLock)
            {
                foreach (byte b in dataTemp)
                {
                    bufferQueue.Enqueue(b);
                }
            }
            //foreach (byte item in dataTemp)
            //{
            //    bufferQueue.Enqueue(item);//数据加入到缓存区队列中   
            //}

            //异步线程解析数据帧
            
                
                    Task.Run(() =>
                    {
                        lock (queueLock)
                        {

                            if (bufferQueue.Count > 0) { ParseDataFrames(); }

                        }
                    });
                
            
        }
        private void ParseDataFrames()
        {
            //解析帧头
            if (isHeadReceive == false)
            {
                while (bufferQueue.Count > 0)
                {

                    if (bufferQueue.Peek() == frame_head1)
                    {
                        bufferQueue.Dequeue();
                        //Console.WriteLine("队列>0，检测帧头");
                        if (bufferQueue.Count > 0 && bufferQueue.Peek() == frame_head2)
                        {
                            bufferQueue.Dequeue();
                            isHeadReceive = true;
                            break;
                        }
                    }
                    else
                    {
                        bufferQueue.Dequeue();
                    }
                }


            }
            //解析数据
            if (isHeadReceive == true)
            {
                while (bufferQueue.Count >= 108 )
                {
                    //Console.WriteLine("清空队列前的字节数："+bufferQueue.Count);
                    //byte[] frame = new byte[bufferQueue.Count];
                    //bufferQueue.CopyTo(frame, 0);//将队列中的数据映射到数组中进行解析
                    //bufferQueue.Clear(); 
                    //Console.WriteLine("清空队列后的字节数："+bufferQueue.Count);

                    byte[] frame = new byte[108];
                    
                    for (int i = 0; i < 108; i++)
                    {
                         frame[i] = bufferQueue.Dequeue();
                    }
                    
                   // Console.WriteLine("复制到frame完成，队列中剩余数据个数为" + bufferQueue.Count);
                    
                    // 检查帧尾
                    if ( frame[frame.Length - 2] == frame_tail1 && frame[frame.Length - 1] == frame_tail2)
                    {
                        //[frame.Length - 2]可以用具体的索引来替换，由于帧头俩字节已出队，则frame[106]为tail1,frame[107]为tial2
                        // 处理完整的数据帧
                        this.Invoke(new Action(() =>
                            {

                                UpdateUI(frame);
                               // Console.WriteLine("UI更新完成，队列中剩余数据个数为：" + bufferQueue.Count);
                            }));


                        isHeadReceive = false;//重新检测帧头
                        
                        saver.SaveToCsv(csvfullPath, frame);
                    }
                    else
                    {
                        isHeadReceive = false;//没有检测到帧尾，重新检测帧头

                    }
                }



            }
        }
        
        //显示系统时间
        private void Timer1_Tick(object sender, EventArgs e)
        {
             label_time.Text = "系统时间：" + DateTime.Now.ToString();
        }
        
        
        //打开运行记录文件所在位置
        private void button_openfile_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(csvFilePath))
            {
                try
                {
                    // 使用 Process.Start 打开文件夹
                    Process.Start(csvFilePath);//新进程打开文件夹
                }
                catch (Exception ex)
                {
                    MessageBox.Show("无法打开文件夹：" + ex.Message);
                }
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(csvFilePath);
                    Process.Start(csvFilePath);//新进程打开文件夹
                }
                catch (Exception ex)
                {
                    MessageBox.Show("文件夹创建失败：" + ex.Message);
                }
                

            }
        }
        //录屏按钮
        private void button_record_Click(object sender, EventArgs e)
        {
            //时间戳生成路径
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss").Replace(":", "-");
            screenfileName = $"screen_{timestamp}.mp4";
            screenfullPath = Path.Combine(csvFilePath, screenfileName);

            if (isRecording)
            {
                isRecording = false;
                recorder.StopRecording();
                button_record.Text = "开始录屏";
                button_record.BackColor = Color.White;

            }
            else
            {
                isRecording = true;
                button_record.Text = "停止录屏";
                button_record.BackColor = Color.Green;
                recorder.StartRecording(screenfullPath);
            }
        }


           //UI更新
            private void UpdateUI(byte[] frame)
        {
            // 在这里处理完整的数据帧
            // 使用StringBuilder来构建16进制字符串

            //StringBuilder hexBuilder = new StringBuilder();
            //foreach (byte b in frame)
            //{
            //    // 使用"X2"格式化字节数据为16进制字符串，确保每个字节以两个16进制数字表示
            //    string hex = b.ToString("X2");
            //    hexBuilder.Append(hex);
            //    // 如果不是最后一个字节，添加一个分隔符（例如空格）
            //    if (b != frame.Last())
            //    {
            //        hexBuilder.Append(" ");
            //    }
            //}
            //string text = hexBuilder.ToString();
            //richTextBox_warning.AppendText(" " + text + Environment.NewLine);





            //转速2字节，温度2字节，推力2字节,故障码1字节，状态1字节,电流1字节 舵机电流1字节，舵机电流1字节。 前8个涡喷每个11个字节 
            //                                                                                                第9、10个涡喷没有舵机电流



            //涡喷1
            int n1 = ((frame[0] << 8) | frame[1])*10;//转速2字节组合
            label_wp1_n.Text = n1.ToString() + "rpm";
            int t1 = (frame[2] << 8) | frame[3];//温度2字节组合
            label_wp1_t.Text = t1.ToString() + " °C";
            int f1 = (frame[4] << 8) | frame[5];//推力2字节组合
            label_wp1_f.Text = f1.ToString() + " N";
            if (Decoder.ErrorsCodeDic.ContainsKey(frame[6]))
            {
                string wp_error = Decoder.ErrorsCodeDic[frame[6]];
                richTextBox_warning.AppendText("涡喷1:" + DateTime.Now.ToString() + wp_error + Environment.NewLine);
            }
            //richTextBox_warning.AppendText("涡喷1:"+Decoder.GetErrors(Decoder.ErrorsCodeDic, frame[6]) + Environment.NewLine);
            label_wp1_s.Text =Decoder.GetStatus(Decoder.StatusCodeDic, frame[7]);//状态码
            double i1 = frame[8] * 0.2; 
            label_wp1_i.Text = i1.ToString() + "  A";//涡喷电流
            label_wp1_dj1.Text = frame[9].ToString() + "  A";//舵机电流1
            label_wp1_dj2.Text = frame[10].ToString() + "  A";//舵机电流2

            //涡喷2
            int n2 = ((frame[11] << 8) | frame[12])* 10;//转速2字节组合
            label_wp2_n.Text = n2.ToString() + "rpm";
            int t2 = (frame[13] << 8) | frame[14];//温度2字节组合
            label_wp2_t.Text = t2.ToString() + " °C";
            int f2 = (frame[15] << 8) | frame[16];//推力2字节组合
            label_wp2_f.Text = f2.ToString() + " N";
            if (Decoder.ErrorsCodeDic.ContainsKey(frame[17]))
            {
                string wp_error = Decoder.ErrorsCodeDic[frame[17]];
                richTextBox_warning.AppendText("涡喷2:"  + wp_error + DateTime.Now.ToString() + Environment.NewLine);
            }
            //richTextBox_warning.AppendText("涡喷2:" + Decoder.GetErrors(Decoder.ErrorsCodeDic, frame[17]) + Environment.NewLine);
            label_wp2_s.Text =Decoder.GetStatus(Decoder.StatusCodeDic, frame[18]);//状态码
            double i2 = frame[19] * 0.2;
            label_wp2_i.Text = i2.ToString() + "  A";//涡喷电流
            label_wp2_dj1.Text = frame[20].ToString() + "  A";//舵机电流1
            label_wp2_dj2.Text = frame[21].ToString() + "  A";//舵机电流2

            //涡喷3
            int n3 = ((frame[22] << 8) | frame[23] )* 10;//转速2字节组合
            label_wp3_n.Text = n3.ToString() + "rpm";
            int t3 = (frame[24] << 8) | frame[25];//温度2字节组合
            label_wp3_t.Text = t3.ToString() + " °C";
            int f3 = (frame[26] << 8) | frame[27];//推力2字节组合
            label_wp3_f.Text = f3.ToString() + " N";
            if (Decoder.ErrorsCodeDic.ContainsKey(frame[28]))
            {
                string wp_error = Decoder.ErrorsCodeDic[frame[28]];
                richTextBox_warning.AppendText("涡喷3:" + DateTime.Now.ToString() + wp_error + Environment.NewLine);
            }
            //richTextBox_warning.AppendText("涡喷3:" + Decoder.GetErrors(Decoder.ErrorsCodeDic, frame[28]) + Environment.NewLine);
            label_wp3_s.Text =Decoder.GetStatus(Decoder.StatusCodeDic, frame[29]);//状态码
            double i3 = frame[30] * 0.2;
            label_wp3_i.Text = i3.ToString() + "  A";//涡喷电流
            label_wp3_dj1.Text = frame[31].ToString() + "  A";//舵机电流1
            label_wp3_dj2.Text = frame[32].ToString() + "  A";//舵机电流2

            //涡喷4
            int n4 = ((frame[33] << 8) | frame[34] )* 10;//转速2字节组合
            label_wp4_n.Text = n4.ToString() + "rpm";
            int t4 = (frame[35] << 8) | frame[36];//温度2字节组合
            label_wp4_t.Text = t4.ToString() + " °C";
            int f4 = (frame[37] << 8) | frame[38];//推力2字节组合
            label_wp4_f.Text = f4.ToString() + " N";
            if (Decoder.ErrorsCodeDic.ContainsKey(frame[39]))
            {
                string wp_error = Decoder.ErrorsCodeDic[frame[39]];
                richTextBox_warning.AppendText("涡喷4:" + DateTime.Now.ToString() + wp_error + Environment.NewLine);
            }
            //richTextBox_warning.AppendText("涡喷4:" + Decoder.GetErrors(Decoder.ErrorsCodeDic, frame[39]) + Environment.NewLine);
            label_wp4_s.Text = Decoder.GetStatus(Decoder.StatusCodeDic, frame[40]);//状态码
            double i4 = frame[41] * 0.2;
            label_wp4_i.Text = i4.ToString() + "  A";//涡喷电流
            label_wp4_dj1.Text = frame[42].ToString() + "  A";//舵机电流1
            label_wp4_dj2.Text = frame[43].ToString() + "  A";//舵机电流2

            //涡喷5
            int n5 = ((frame[44] << 8) | frame[45] )* 10;//转速2字节组合
            label_wp5_n.Text = n5.ToString() + "rpm";
            int t5 = (frame[46] << 8) | frame[47];//温度2字节组合
            label_wp5_t.Text = t5.ToString() + " °C";
            int f5 = (frame[48] << 8) | frame[49];//推力2字节组合
            label_wp5_f.Text = f5.ToString() + " N";
            if (Decoder.ErrorsCodeDic.ContainsKey(frame[50]))
            {
                string wp_error = Decoder.ErrorsCodeDic[frame[50]];
                richTextBox_warning.AppendText("涡喷5:" + DateTime.Now.ToString() + wp_error + Environment.NewLine);
            }
            //richTextBox_warning.AppendText("涡喷5:" + Decoder.GetErrors(Decoder.ErrorsCodeDic, frame[50]) + Environment.NewLine);
            label_wp5_s.Text =Decoder.GetStatus(Decoder.StatusCodeDic, frame[51]);//状态码
            double i5 = frame[52] * 0.2;
            label_wp5_i.Text = i5.ToString() + "  A";//涡喷电流
            label_wp5_dj1.Text = frame[53].ToString() + "  A";//舵机电流1
            label_wp5_dj2.Text = frame[54].ToString() + "  A";//舵机电流2

            //涡喷6
            int n6 = ((frame[55] << 8) | frame[56] )* 10;//转速2字节组合
            label_wp6_n.Text = n6.ToString() + "rpm";
            int t6 = (frame[57] << 8) | frame[58];//温度2字节组合
            label_wp6_t.Text = t6.ToString() + " °C";
            int f6 = (frame[59] << 8) | frame[60];//推力2字节组合
            label_wp6_f.Text = f6.ToString() + " N";
            if (Decoder.ErrorsCodeDic.ContainsKey(frame[61]))
            {
                string wp_error = Decoder.ErrorsCodeDic[frame[61]];
                richTextBox_warning.AppendText("涡喷6:" + DateTime.Now.ToString() + wp_error + Environment.NewLine);
            }
            //richTextBox_warning.AppendText("涡喷6:" + Decoder.GetErrors(Decoder.ErrorsCodeDic, frame[61]) + Environment.NewLine);
            label_wp6_s.Text = Decoder.GetStatus(Decoder.StatusCodeDic, frame[62]);//状态码
            double i6 = frame[63] * 0.2;            //涡喷电流
            label_wp6_i.Text = i6.ToString() + "  A";
            label_wp6_dj1.Text = frame[64].ToString() + "  A";//舵机电流1
            label_wp6_dj2.Text = frame[65].ToString() + "  A";//舵机电流2

            //涡喷7
            int n7 = ((frame[66] << 8) | frame[67] )* 10;//转速2字节组合
            label_wp7_n.Text = n7.ToString() + "rpm";
            int t7 = (frame[68] << 8) | frame[69];//温度2字节组合
            label_wp7_t.Text = t7.ToString() + " °C";
            int f7 = (frame[70] << 8) | frame[71];//推力2字节组合
            label_wp7_f.Text = f7.ToString() + " N";
            if (Decoder.ErrorsCodeDic.ContainsKey(frame[72]))
            {
                string wp_error = Decoder.ErrorsCodeDic[frame[72]];
                richTextBox_warning.AppendText("涡喷7:" + DateTime.Now.ToString() + wp_error + Environment.NewLine);
            }
            //richTextBox_warning.AppendText("涡喷7:" + Decoder.GetErrors(Decoder.ErrorsCodeDic, frame[72]) + Environment.NewLine);
            label_wp7_s.Text = Decoder.GetStatus(Decoder.StatusCodeDic, frame[73]);//状态码
            double i7 = frame[74] * 0.2;            //涡喷电流
            label_wp7_i.Text = i7.ToString() + "  A";
            label_wp7_dj1.Text = frame[75].ToString() + "  A";//舵机电流1
            label_wp7_dj2.Text = frame[76].ToString() + "  A";//舵机电流2

            //涡喷8
            int n8 = ((frame[77] << 8) | frame[78] )* 10;//转速2字节组合
            label_wp8_n.Text = n8.ToString() + "rpm";
            int t8 = (frame[79] << 8) | frame[80];//温度2字节组合
            label_wp8_t.Text = t8.ToString() + " °C";
            int f8 = (frame[81] << 8) | frame[82];//推力2字节组合
            label_wp8_f.Text = f8.ToString() + " N";
            if (Decoder.ErrorsCodeDic.ContainsKey(frame[83]))
            {
                string wp_error = Decoder.ErrorsCodeDic[frame[83]];
                richTextBox_warning.AppendText("涡喷8:" + DateTime.Now.ToString() + wp_error + Environment.NewLine);
            }
            //richTextBox_warning.AppendText("涡喷8:" + Decoder.GetErrors(Decoder.ErrorsCodeDic, frame[83]) + Environment.NewLine);
            label_wp8_s.Text = Decoder.GetStatus(Decoder.StatusCodeDic, frame[84]);//状态码
            double i8 = frame[85] * 0.2;            //涡喷电流
            label_wp8_i.Text = i8.ToString() + "  A";
            label_wp8_dj1.Text = frame[86].ToString() + "  A";//舵机电流1
            label_wp8_dj2.Text = frame[87].ToString() + "  A";//舵机电流2

            //涡喷9
            int n9 = ((frame[88] << 8) | frame[89] )* 10;//转速2字节组合
            label_wp9_n.Text = n9.ToString() + "rpm";
            int t9 = (frame[90] << 8) | frame[91];//温度2字节组合
            label_wp9_t.Text = t9.ToString() + " °C";
            int f9 = (frame[92] << 8) | frame[93];//推力2字节组合
            label_wp9_f.Text = f9.ToString() + " N";
            if (Decoder.ErrorsCodeDic.ContainsKey(frame[94]))
            {
                string wp_error = Decoder.ErrorsCodeDic[frame[94]];
                richTextBox_warning.AppendText("涡喷9:" + DateTime.Now.ToString() + wp_error + Environment.NewLine);
            }
            //richTextBox_warning.AppendText("涡喷9:" + Decoder.GetErrors(Decoder.ErrorsCodeDic, frame[94]) + Environment.NewLine);
            label_wp9_s.Text = Decoder.GetStatus(Decoder.StatusCodeDic, frame[95]);//状态码
            double i9 = frame[96] * 0.2;            //涡喷电流
            label_wp9_i.Text = i9.ToString() + "  A";


            //涡喷10
            int n10 = ((frame[97] << 8) | frame[98]) * 10;//转速2字节组合
            label_wp10_n.Text = n10.ToString() + "rpm";
            int t10 = (frame[99] << 8) | frame[100];//温度2字节组合
            label_wp10_t.Text = t10.ToString() + " °C";
            int f10 = (frame[101] << 8) | frame[102];//推力2字节组合
            label_wp10_f.Text = f10.ToString() + " N";
            if (Decoder.ErrorsCodeDic.ContainsKey(frame[103]))
            {
                string wp_error = Decoder.ErrorsCodeDic[frame[103]];
                richTextBox_warning.AppendText("涡喷10:" + DateTime.Now.ToString() + wp_error + Environment.NewLine);
            }
            //richTextBox_warning.AppendText("涡喷10:" + Decoder.GetErrors(Decoder.ErrorsCodeDic, frame[103]) + Environment.NewLine);
            label_wp10_s.Text = Decoder.GetStatus(Decoder.StatusCodeDic, frame[104]);//状态码
            double i10 = frame[105] * 0.2;            //涡喷电流
            label_wp10_i.Text = i10.ToString() + "  A";

            richTextBox_warning.SelectionStart = richTextBox_warning.TextLength; // 设置光标的位置到文本末尾
            richTextBox_warning.ScrollToCaret();// 滚动到控件光标处



        }




        //清空UI

        private void UIclear()
        {

            richTextBox_warning.Clear();// 报警框

            //转速2字节，温度2字节，状态1字节，故障码1字节，油门1字节，推力1字节、电流1字节。16个舵机电流，每个1字节。 


            //涡喷1
            label_wp1_n.Text = "";//转速
            label_wp1_t.Text = "";//温度
            label_wp1_s.Text = "";//状态码
            label_wp1_f.Text = ""; //推力
            label_wp1_dj1.Text = "";//舵机电流1
            label_wp1_dj2.Text = "";//舵机电流2

            //涡喷2
            label_wp2_n.Text = "";
            label_wp2_t.Text = "";
            label_wp2_s.Text = "";//状态码
            label_wp2_f.Text = "";
            label_wp2_dj1.Text = "";
            label_wp2_dj2.Text = "";

            //涡喷3
            label_wp3_n.Text = "";
            label_wp3_t.Text = "";
            label_wp3_s.Text = "";
            label_wp3_f.Text = "";
            label_wp3_dj1.Text = "";
            label_wp3_dj2.Text = "";

            //涡喷4
            label_wp4_n.Text = "";
            label_wp4_t.Text = "";
            label_wp4_s.Text = "";
            label_wp4_f.Text = "";
            label_wp4_dj1.Text = "";
            label_wp4_dj2.Text = "";

            //涡喷5
            label_wp5_n.Text = "";
            label_wp5_t.Text = "";
            label_wp5_s.Text = "";
            label_wp5_f.Text = "";
            label_wp5_dj1.Text = "";
            label_wp5_dj2.Text = "";

            //涡喷6
            label_wp6_n.Text = "";
            label_wp6_t.Text = "";
            label_wp6_s.Text = "";
            label_wp6_f.Text = "";
            label_wp6_dj1.Text = "";
            label_wp6_dj2.Text = "";

            //涡喷7
            label_wp7_n.Text = "";
            label_wp7_t.Text = "";
            label_wp7_s.Text = "";
            label_wp7_f.Text = "";
            label_wp7_dj1.Text = "";
            label_wp7_dj2.Text = "";

            //涡喷8
            label_wp8_n.Text = "";
            label_wp8_t.Text = "";
            label_wp8_s.Text = "";
            label_wp8_f.Text = "";
            label_wp8_dj1.Text = "";
            label_wp8_dj2.Text = "";

            //涡喷9
            label_wp9_n.Text = "";
            label_wp9_t.Text = "";
            label_wp9_s.Text = "";
            label_wp9_f.Text = "";


            //涡喷10
            label_wp10_n.Text = "";
            label_wp10_t.Text = "";
            label_wp10_s.Text = "";
            label_wp10_f.Text = "";



        }


    }
}
