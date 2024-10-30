using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMI
{

    public class DataSaver
    {
        public void WriteCsvTitle(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, append: true, Encoding.UTF8))
                {
                    writer.WriteLine("时间,涡喷1转速,转速,涡喷1温度,温度,涡喷1状态,涡喷1故障码,涡喷1油门,涡喷1推力,涡喷1电流," +
                        "涡喷1转速,转速,涡喷1温度,温度,涡喷1状态,涡喷1故障码,涡喷1油门,涡喷1推力,涡喷1电流," +
                        "涡喷1转速,转速,涡喷1温度,温度,涡喷1状态,涡喷1故障码,涡喷1油门,涡喷1推力,涡喷1电流," +
                        "涡喷1转速,转速,涡喷1温度,温度,涡喷1状态,涡喷1故障码,涡喷1油门,涡喷1推力,涡喷1电流," +
                        "涡喷1转速,转速,涡喷1温度,温度,涡喷1状态,涡喷1故障码,涡喷1油门,涡喷1推力,涡喷1电流," +
                        "涡喷1转速,转速,涡喷1温度,温度,涡喷1状态,涡喷1故障码,涡喷1油门,涡喷1推力,涡喷1电流," +
                        "涡喷1转速,转速,涡喷1温度,温度,涡喷1状态,涡喷1故障码,涡喷1油门,涡喷1推力,涡喷1电流," +
                        "涡喷1转速,转速,涡喷1温度,温度,涡喷1状态,涡喷1故障码,涡喷1油门,涡喷1推力,涡喷1电流," +
                        "涡喷1转速,转速,涡喷1温度,温度,涡喷1状态,涡喷1故障码,涡喷1油门,涡喷1推力,涡喷1电流," +
                        "涡喷1转速,转速,涡喷1温度,温度,涡喷1状态,涡喷1故障码,涡喷1油门,涡喷1推力,涡喷1电流," +
                        "涡喷1转速,转速,涡喷1温度,温度,涡喷1状态,涡喷1故障码,涡喷1油门,涡喷1推力,涡喷1电流," +
                        "涡喷1转速,转速,涡喷1温度,温度,涡喷1状态,涡喷1故障码,涡喷1油门,涡喷1推力,涡喷1电流,");
                }
            }
            catch (IOException ex) when ((ex.HResult & 0x0000FFFF) == 32)
            {
                Console.WriteLine("文件正在被另一个进程使用，请稍后再试。");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"发生I/O错误: {ex.Message}");
            }



        }
        public void SaveToCsv(string filePath, byte[] data)
        {
            // 将字节数组转换为字符串;
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 获取当前时间戳
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            // 使用StringBuilder构建CSV行

            StringBuilder csvLine = new StringBuilder();
            csvLine.Append($"\"{timestamp}\""); // 添加时间戳

            // 遍历字节数组，将每个字节转换为10进制并添加到不同的列
            for (int i = 0; i < data.Length; i++)
            {
                // 如果不是第一个元素，添加一个逗号作为分隔符
                
                
                 csvLine.Append($",{data[i]}");
                
            }
            string csvRow = csvLine.ToString();
            
            //foreach (byte b in data)
            //{
            //    csvLine.Append($",{b}");
            //}

            // 使用StreamWriter写入CSV文件
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, append: true, Encoding.UTF8))
                {
                    writer.WriteLine(csvRow);
                    writer.Flush();
                }
            }
            catch (IOException ex) when ((ex.HResult & 0x0000FFFF) == 32)
            {
                Console.WriteLine("文件正在被另一个进程使用，请稍后再试。");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"发生I/O错误: {ex.Message}");
            }
            





        }
    }
}
    

