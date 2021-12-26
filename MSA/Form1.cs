using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Windows.Forms.DataVisualization.Charting;
using Accord.Math.Decompositions;
using CenterSpace.NMath;
using CenterSpace.NMath.Stats;
using CenterSpace.NMath.Core;


namespace MSA
{
    public partial class Form1 : Form
    {
        static int size_DataBuffer_SP = 1280;
        Byte[] DataBuffer_SP = new Byte[size_DataBuffer_SP];
        static int counter_receiveBytes = 0, counter_collectSamples=0;
        bool flag_recording = false;
        short[] dataBuffer = new short[8];//准备填入到Channel1~Channel8的缓存数组
        static int size_dataToPlot = 10000;
        myCircleQueue<double> EMG_CH1 = new myCircleQueue<double>(size_dataToPlot);
        myCircleQueue<double> EMG_CH2 = new myCircleQueue<double>(size_dataToPlot);
        myCircleQueue<double> EMG_CH3 = new myCircleQueue<double>(size_dataToPlot);
        myCircleQueue<double> EMG_CH4 = new myCircleQueue<double>(size_dataToPlot);
        myCircleQueue<double> EMG_CH5 = new myCircleQueue<double>(size_dataToPlot);
        myCircleQueue<double> EMG_CH6 = new myCircleQueue<double>(size_dataToPlot);
        myCircleQueue<double> EMG_CH7 = new myCircleQueue<double>(size_dataToPlot);
        myCircleQueue<double> EMG_CH8 = new myCircleQueue<double>(size_dataToPlot);
        static int emg_windowSize = 200;
        myCircleQueue<double> EMG_CH1_withinWindow = new myCircleQueue<double>(emg_windowSize);
        myCircleQueue<double> EMG_CH2_withinWindow = new myCircleQueue<double>(emg_windowSize);
        myCircleQueue<double> EMG_CH3_withinWindow = new myCircleQueue<double>(emg_windowSize);
        myCircleQueue<double> EMG_CH4_withinWindow = new myCircleQueue<double>(emg_windowSize);
        myCircleQueue<double> EMG_CH5_withinWindow = new myCircleQueue<double>(emg_windowSize);
        myCircleQueue<double> EMG_CH6_withinWindow = new myCircleQueue<double>(emg_windowSize);
        myCircleQueue<double> EMG_CH7_withinWindow = new myCircleQueue<double>(emg_windowSize);
        myCircleQueue<double> EMG_CH8_withinWindow = new myCircleQueue<double>(emg_windowSize);
        myCircleQueue<double>[] EMG_withinWindow = new myCircleQueue<double>[8];
        myCircleQueue<double>[] EMGPredictData = new myCircleQueue<double>[8];
        // EMG data
        const int num_samples = 90000;
        double[,] EMGData = new double[4, num_samples];
        double[,] EMGData_filtered = new double[4, num_samples];
        double[,] EMG_activation = new double[num_samples, 4];

        public Form1()
        {
            InitializeComponent();
            int k;
            string[] port_names = SerialPort.GetPortNames(); // 获取所有可用串口的名字
            for (k = 0; k < port_names.Length; k++)
            {
                comboBox1.Items.Add(port_names[k]);
            }
            serialPort1.Close();
            //该语句相当于QT中的connect,serialPortDataReceived1为串口接收到数据触发的slot槽函数
            serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPortDataReceived1);

            // 填充0 
            for (int i = 0; i < size_dataToPlot; i++)
            {
                EMG_CH1.myEnQueue(0.0);
                EMG_CH2.myEnQueue(0.0);
                EMG_CH3.myEnQueue(0.0);
                EMG_CH4.myEnQueue(0.0);
                EMG_CH5.myEnQueue(0.0);
                EMG_CH6.myEnQueue(0.0);
                EMG_CH7.myEnQueue(0.0);
                EMG_CH8.myEnQueue(0.0);
            }

            // 初始化图表
            initializeFigures();
            axWindowsMediaPlayer1.Visible = false;
        }

        private void initializeFigures()
        {
            /* 
             * chart activation
             */
            int[] arr = new int[] { 94, 52, 25, 67, 91, 56, 21, 77, 99, 56, 26, 77, 69, 56, 29, 37 };
            chart_activation.Series.Clear();  //清除默认的Series
            Series Strength = new Series("通道1");
            Strength.ChartType = SeriesChartType.Spline;  //设置chart的类型，spline样条图 Line折线图
            Strength.IsValueShownAsLabel = false; //把值当做标签展示（默认false）
            chart_activation.ChartAreas[0].AxisX.MajorGrid.Interval = 1;  //设置网格间隔（这里设成0.5，看得更直观一点）
            for (int i = 1; i <= arr.Length; i++)
            {
            Strength.Points.AddXY(i, arr[i - 1]);
            }
            //把series添加到chart上
            chart_activation.Series.Add(Strength);
            /* 
             * chart synergy matrix 
             */
            chart_synergyMatrix.Series.Clear();  //清除默认的Series
            Series Salary = new Series("Salary");
            Salary.Points.AddXY("Ajay", "10000");
            Salary.Points.AddXY("Ramesh", "8000");
            Salary.Points.AddXY("Ankit", "7000");
            Salary.Points.AddXY("Suresh", "8500");
            chart_synergyMatrix.Series.Add(Salary);
            /* 
             * chart activation
             */
            int[] arr22 = new int[] { 94, 52, 25, 67, 91, 56, 21, 77, 99, 56, 26, 77, 69, 56, 29, 37 };
            chart_activationFactor.Series.Clear();  //清除默认的Series
            Series Strength2 = new Series("通道1");
            Strength2.ChartType = SeriesChartType.Spline;  //设置chart的类型，spline样条图 Line折线图
            Strength2.IsValueShownAsLabel = false; //把值当做标签展示（默认false）
            chart_activation.ChartAreas[0].AxisX.MajorGrid.Interval = 2;  //设置网格间隔（这里设成0.5，看得更直观一点）
            for (int i = 1; i <= arr.Length; i++)
            {
                Strength2.Points.AddXY(i, arr[i - 1]);
            }
            //把series添加到chart上
            chart_activationFactor.Series.Add(Strength);
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        //串口接受数据（中断）
        private void serialPortDataReceived1(object sender, SerialDataReceivedEventArgs e)
        {
            int DataCount1 = serialPort1.BytesToRead;
            Console.WriteLine("DataCount1:" + DataCount1.ToString());
            byte[] readBuffer1 = new byte[DataCount1];
            serialPort1.Read(readBuffer1, 0, DataCount1);
            for (int i = 0; i < DataCount1; i++)
            {
                DataBuffer_SP[counter_receiveBytes] = readBuffer1[i];
                counter_receiveBytes++;
                if (counter_receiveBytes == size_DataBuffer_SP)
                {
                    counter_receiveBytes = 0;
                }
                if (counter_receiveBytes < 16)
                {
                    if (DataBuffer_SP[((counter_receiveBytes - 2) % size_DataBuffer_SP + size_DataBuffer_SP) % size_DataBuffer_SP] == 0x0D &&
                        DataBuffer_SP[((counter_receiveBytes - 1) % size_DataBuffer_SP + size_DataBuffer_SP) % size_DataBuffer_SP] == 0x0A)
                    {

                        for (int j = 0; j < 8; j++)
                        {
                            dataBuffer[j] = (short)((short)DataBuffer_SP[((counter_receiveBytes - 16 + j) % size_DataBuffer_SP + size_DataBuffer_SP) % size_DataBuffer_SP] << 4);//读取通道j的高8位    
                            if ((j % 2) != 0)
                            {
                                dataBuffer[j] += (short)(DataBuffer_SP[((counter_receiveBytes - 8 + j / 2) % size_DataBuffer_SP + size_DataBuffer_SP) % size_DataBuffer_SP] & 0x0F);
                            }
                            else
                            {
                                dataBuffer[j] += (short)((DataBuffer_SP[((counter_receiveBytes - 8 + j / 2) % size_DataBuffer_SP + size_DataBuffer_SP) % size_DataBuffer_SP] & 0xF0) >> 4);
                            }
                            if ((dataBuffer[j] & 0x0800) != 0)//转换为有符号数
                            {
                                dataBuffer[j] = (short)(dataBuffer[j] | 0xF000);
                            }
                        }

                        //将dataBuffer中的数据搬运到EMG_CH1~EMG_CH8中，用于显示
                        EMG_CH1.myEnQueue(dataBuffer[0]);
                        EMG_CH2.myEnQueue(dataBuffer[1]);
                        EMG_CH3.myEnQueue(dataBuffer[2]);
                        EMG_CH4.myEnQueue(dataBuffer[3]);
                        EMG_CH5.myEnQueue(dataBuffer[4]);
                        EMG_CH6.myEnQueue(dataBuffer[5]);
                        EMG_CH7.myEnQueue(dataBuffer[6]);
                        EMG_CH8.myEnQueue(dataBuffer[7]);
                        //将dataBuffer中的数据刷入EMG_CH1_withinWindow~EMG_CH8_withinWindow中，用于实时提取特征并解码
                        EMG_CH1_withinWindow.myEnQueue(dataBuffer[0]);
                        EMG_CH2_withinWindow.myEnQueue(dataBuffer[1]);
                        EMG_CH3_withinWindow.myEnQueue(dataBuffer[2]);
                        EMG_CH4_withinWindow.myEnQueue(dataBuffer[3]);
                        EMG_CH5_withinWindow.myEnQueue(dataBuffer[4]);
                        EMG_CH6_withinWindow.myEnQueue(dataBuffer[5]);
                        EMG_CH7_withinWindow.myEnQueue(dataBuffer[6]);
                        EMG_CH8_withinWindow.myEnQueue(dataBuffer[7]);
                        if (flag_recording)
                        {
                            recordSampleData(dataBuffer);
                        }
                    }
                }
                else  //counter_receiveBytes >= 16
                {
                    if (DataBuffer_SP[counter_receiveBytes - 1] == 0x0A && DataBuffer_SP[counter_receiveBytes - 2] == 0x0D)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            dataBuffer[j] = (short)((short)DataBuffer_SP[counter_receiveBytes - 16 + j] << 4);
                            if ((j % 2) != 0)
                            {
                                dataBuffer[j] += (short)(DataBuffer_SP[counter_receiveBytes - 8 + j / 2] & 0x0F);
                            }
                            else
                            {
                                dataBuffer[j] += (short)((DataBuffer_SP[counter_receiveBytes - 8 + j / 2] & 0xF0) >> 4);

                            }
                            if ((dataBuffer[j] & 0x0800) != 0)//转换为有符号数
                            {
                                dataBuffer[j] = (short)(dataBuffer[j] | 0xF000);
                            }
                        }

                        //将dataBuffer中的数据搬运到EMG_CH1~EMG_CH8中
                        EMG_CH1.myEnQueue(dataBuffer[0]);
                        EMG_CH2.myEnQueue(dataBuffer[1]);
                        EMG_CH3.myEnQueue(dataBuffer[2]);
                        EMG_CH4.myEnQueue(dataBuffer[3]);
                        EMG_CH5.myEnQueue(dataBuffer[4]);
                        EMG_CH6.myEnQueue(dataBuffer[5]);
                        EMG_CH7.myEnQueue(dataBuffer[6]);
                        EMG_CH8.myEnQueue(dataBuffer[7]);
                        //将dataBuffer中的数据刷入EMG_CH1_withinWindow~EMG_CH8_withinWindow中，用于实时提取特征并解码
                        EMG_CH1_withinWindow.myEnQueue(dataBuffer[0]);
                        EMG_CH2_withinWindow.myEnQueue(dataBuffer[1]);
                        EMG_CH3_withinWindow.myEnQueue(dataBuffer[2]);
                        EMG_CH4_withinWindow.myEnQueue(dataBuffer[3]);
                        EMG_CH5_withinWindow.myEnQueue(dataBuffer[4]);
                        EMG_CH6_withinWindow.myEnQueue(dataBuffer[5]);
                        EMG_CH7_withinWindow.myEnQueue(dataBuffer[6]);
                        EMG_CH8_withinWindow.myEnQueue(dataBuffer[7]);
                        if (flag_recording)
                        {
                            recordSampleData(dataBuffer);
                        }
                    }
                }
            }
        }


        //开启or关闭串口
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "开启串口")
            {
                DateTime beforDT = System.DateTime.Now;
                if (comboBox1.Text == "")
                {
                    MessageBox.Show("请选择串口号！", "提示！");
                }
                else
                {
                    //serialPort1.Close();
                    serialPort1.PortName = comboBox1.Text;
                    try
                    {
                        serialPort1.BaudRate = 115200;//115200;
                        serialPort1.ReceivedBytesThreshold = 16;
                        //Thread.Sleep(1000);   
                        serialPort1.Open();
                        button1.Text = "关闭串口";
                        startEMGButton.Enabled = true;
                        stopEMGButton.Enabled = false;
                        saveEMGButton.Enabled = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
                DateTime afterDT = System.DateTime.Now;
                TimeSpan ts = afterDT.Subtract(beforDT);
                Console.WriteLine("开启串口总共花费{0}ms.", ts.TotalMilliseconds);
            }
            else
            {
                serialPort1.Close();
                button1.Text = "开启串口";
                startEMGButton.Enabled = false;
                stopEMGButton.Enabled = false;
                saveEMGButton.Enabled = false;
                NMFButton.Enabled = false;
            }
        }

        
        //开始肌电采集
        private void startEMGButton_Click(object sender, EventArgs e)
        {
            timer_plotData.Start();
            if (serialPort1.IsOpen)
            {
                //byte[] data = new byte[10] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                //serialPort1.Write(data, 0, data.Length);
                startEMGButton.Enabled = false;
                stopEMGButton.Enabled = true;
                importDataButton.Enabled = true;
                serialPort1.DiscardInBuffer();
                //DataBuffer_SP.Clear();
                counter_receiveBytes = 0;
            }
            else if (serialPort1.IsOpen == false)
            {
                MessageBox.Show("请先连接设备！", "提示！");
            }
        }

        // 导入肌电数据
        private void importDataButton_Click(object sender, EventArgs e)
        {
            openFileDialog_EMGData.Filter = @"文本文件|*.txt";
            openFileDialog_EMGData.Title = "请选择肌电数据";
            openFileDialog_EMGData.ShowDialog();
            string openFilePath = openFileDialog_EMGData.FileName;
            Console.WriteLine(openFilePath);
            StreamReader sr = new StreamReader(openFilePath, Encoding.Default);
            string content;
            string[] strArr;
            int col_num = 0;
            while (col_num < num_samples)
            {
                if ((content = sr.ReadLine()) != null)
                {
                    strArr = content.Split('\t');     //string[8],最后一个是回车字符\n
                    for (int i = 0; i < 4; i++) //取strArr[4]~strArr[7]
                    {
                        EMGData[i, col_num] = double.Parse(strArr[i+4]);
                    }
                    col_num++;
                }
                else
                {
                    break;
                }
            }
            Console.WriteLine("load file!");
            computeActivation();
            plot_chartActivation(EMG_activation);
        }

        public void plot_chartActivation(double[,] data)
        {
            /* 
             * chart activation
             */
            chart_activation.Series.Clear();  //清除默认的Series
            Series ch1 = new Series("通道1");
            Series ch2 = new Series("通道2");
            Series ch3 = new Series("通道3");
            Series ch4 = new Series("通道4");
            ch1.ChartType = SeriesChartType.Spline;  //设置chart的类型，spline样条图 Line折线图
            ch2.ChartType = SeriesChartType.Spline;
            ch3.ChartType = SeriesChartType.Spline;
            ch4.ChartType = SeriesChartType.Spline;
            chart_activation.ChartAreas[0].AxisX.MajorGrid.Interval = 1;  //设置网格间隔（这里设成0.5，看得更直观一点）
            for (int i = 0; i < num_samples; i++)
            {
                if (i % 100 == 0)
                {
                    ch1.Points.AddXY((double)i, data[i, 0]);
                    ch2.Points.AddXY((double)i, data[i, 1]);
                    ch3.Points.AddXY((double)i, data[i, 2]);
                    ch4.Points.AddXY((double)i, data[i, 3]);
                }
            }
            //把series添加到chart上
            Console.WriteLine("start plot data");
            chart_activation.Series.Add(ch1);
            chart_activation.Series.Add(ch2);
            chart_activation.Series.Add(ch3);
            chart_activation.Series.Add(ch4);
        }


        /// <summary>
        /// 一维数组转2维数组(矩阵)
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="len">矩阵行数</param>
        /// <returns></returns>
        public static T[,] OneD_2<T>(T[] obj, int len)
        {
            if (obj.Length % len != 0)
                return null;
            int width = obj.Length / len;
            T[,] obj2 = new T[len, width];
            for (int i = 0; i < obj.Length; i++)
            {
                obj2[i / width, i % width] = obj[i];
            }
            return obj2;
        }

        /// <summary>
        /// 二维数组转一维数组
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T[] TwoD_1<T>(T[,] obj)
        {
            T[] obj2 = new T[obj.Length];
            for (int i = 0; i < obj.Length; i++)
                obj2[i] = obj[i / obj.GetLength(1), i % obj.GetLength(1)];
            return obj2;
        }

        //传入一个数组,求出一个数组的最大值
        public static T MaxValue<T>(T[] arr) where T : IComparable<T>
        {
            var i_Pos = 0;
            var value = arr[0];
            for (var i = 1; i < arr.Length; ++i)
            {
                var _value = arr[i];
                if (_value.CompareTo(value) > 0)
                {
                    value = _value;
                    i_Pos = i;
                }
            }
            return value;
        }

        // 计算肌肉激活度
        private void computeActivation()
        {
            double[] activation = new double[num_samples * 4];
            for (int idx = 0; idx < 4; idx++)
            {
                for (int jdx = 0; jdx < num_samples; jdx++)
                {
                    EMGData[idx,jdx] = Math.Abs(EMGData[idx, jdx]);
                }
            }
            double[] EMGData_1d = TwoD_1(EMGData);

            //for (int jdx = 0; jdx < num_samples; jdx++)
            //{
            //    Console.Write(EMGData_1d[jdx].ToString() + "\t" + EMGData[0, jdx].ToString() + "\n");
            //}

            for (int _col = 0; _col < 4; _col++)
            {
                double[] arr1 = new double[num_samples];
                Array.Copy(EMGData_1d, _col * num_samples, arr1, 0, num_samples);
                double[] arr_filterd = Butterworth(arr1, 0.001, 2); // 2hz 巴特沃斯低通滤波 模拟肌肉低通滤波器特性
                Array.Copy(arr_filterd, 0, activation, _col * num_samples, num_samples);
            }

            EMGData_filtered = OneD_2(activation, 4);

            //for (int idx = 0; idx < 4; idx++)
            //{
            //    for (int jdx = 0; jdx < num_samples; jdx++)
            //    {
            //        Console.Write(EMGData_filtered[idx, jdx].ToString() + "\t");
            //    }
            //    Console.Write("\n");
            //}

            double _mvc = MaxValue(activation);
            int d = 10;
            double c1 = 0.5;
            double c2 = 0.5;
            double beta1 = c1 + c2;
            double beta2 = c1 * c2;
            double alpha = 1 + beta1 + beta2;
            double A = -1.5;
            for (int _i = 0; _i < 4; _i++)
            {
                for (int _j = 0; _j < num_samples; _j++)
                {
                    EMGData_filtered[_i, _j] = EMGData_filtered[_i, _j] / _mvc;
                    if (_j < d)
                    {
                        EMG_activation[_j, _i] = 0;
                    }
                    else
                    {
                        EMG_activation[_j, _i] = alpha * EMGData_filtered[_i, _j - d] - beta1 * EMG_activation[_j - 1, _i] - beta2 * EMG_activation[_j - 2, _i ];
                    }
                } 
            }

            for (int _i = 0; _i < 4; _i++)
            {
                for (int _j = 0; _j < num_samples; _j++)
                {
                    EMG_activation[_j, _i] = (Math.Exp(A * EMG_activation[_j, _i]) - 1) / (Math.Exp(A) - 1);  // 肌肉激活程度
                }
            }
        }

        public static double[] Butterworth(double[] indata, double deltaTimeinsec, double CutOff)
        {
            if (indata == null) return null;
            if (CutOff == 0) return indata;

            double Samplingrate = 1 / deltaTimeinsec;
            long dF2 = indata.Length - 1;        // The data range is set with dF2
            double[] Dat2 = new double[dF2 + 4]; // Array with 4 extra points front and back
            double[] data = indata; // Ptr., changes passed data

            // Copy indata to Dat2
            for (long r = 0; r < dF2; r++)
            {
                Dat2[2 + r] = indata[r];
            }
            Dat2[1] = Dat2[0] = indata[0];
            Dat2[dF2 + 3] = Dat2[dF2 + 2] = indata[dF2];

            const double pi = 3.14159265358979;
            double wc = Math.Tan(CutOff * pi / Samplingrate);
            double k1 = 1.414213562 * wc; // Sqrt(2) * wc
            double k2 = wc * wc;
            double a = k2 / (1 + k1 + k2);
            double b = 2 * a;
            double c = a;
            double k3 = b / k2;
            double d = -2 * a + k3;
            double e = 1 - (2 * a) - k3;

            // RECURSIVE TRIGGERS - ENABLE filter is performed (first, last points constant)
            double[] DatYt = new double[dF2 + 4];
            DatYt[1] = DatYt[0] = indata[0];
            for (long s = 2; s < dF2 + 2; s++)
            {
                DatYt[s] = a * Dat2[s] + b * Dat2[s - 1] + c * Dat2[s - 2]
                           + d * DatYt[s - 1] + e * DatYt[s - 2];
            }
            DatYt[dF2 + 3] = DatYt[dF2 + 2] = DatYt[dF2 + 1];

            // FORWARD filter
            double[] DatZt = new double[dF2 + 2];
            DatZt[dF2] = DatYt[dF2 + 2];
            DatZt[dF2 + 1] = DatYt[dF2 + 3];
            for (long t = -dF2 + 1; t <= 0; t++)
            {
                DatZt[-t] = a * DatYt[-t + 2] + b * DatYt[-t + 3] + c * DatYt[-t + 4]
                            + d * DatZt[-t + 1] + e * DatZt[-t + 2];
            }

            // Calculated points copied for return
            for (long p = 0; p < dF2; p++)
            {
                data[p] = DatZt[p];
            }

            return data;
        }

        // 定时器响应函数
        private void timer_plotData_Tick(object sender, EventArgs e)
        {
            waveformPlot1.PlotY(EMG_CH1.toArray());
            waveformPlot2.PlotY(EMG_CH2.toArray());
            waveformPlot3.PlotY(EMG_CH3.toArray());
            waveformPlot4.PlotY(EMG_CH4.toArray());
            //waveformPlot5.PlotY(EMG_CH5.toArray());
            //waveformPlot6.PlotY(EMG_CH6.toArray());
            //waveformPlot7.PlotY(EMG_CH7.toArray());
            //waveformPlot8.PlotY(EMG_CH8.toArray());
        }



        private void groupBox7_Enter(object sender, EventArgs e)
        {

        }

        private void printMatrix<T>(T[,] matrix)
        {
            for (int idx = 0; idx < matrix.GetLength(0); idx++)
            {
                for (int jdx = 0; jdx < matrix.GetLength(1); jdx++)
                {
                    Console.Write(matrix[idx, jdx].ToString() + "\t");
                }
                Console.Write("\n");
            }
        }

        private void NMFButton_MouseClick(object sender, MouseEventArgs e)
        {
            DoubleMatrix V = new DoubleMatrix(EMG_activation);
            var fact = new NMFact();
            fact.Factor(V.Transpose(), 2);
            var W = fact.W.ToArray();
            var H = fact.H.ToArray();
            plot_SynergyMatrix(W, W.GetLength(1));
            plot_activationFactor(H, H.GetLength(0));
        }

        public void plot_SynergyMatrix(double[,] matrix, int sy_num)
        {
            /* 
             * chart activation
             */
            chart_synergyMatrix.Series.Clear();  //清除默认的Series
            for (int _sy = 0; _sy < sy_num; _sy++)
            {
                Series sy = new Series("协同" + (_sy + 1).ToString());
                sy.Points.AddXY("通道1", matrix[0, _sy]);
                sy.Points.AddXY("通道2", matrix[1, _sy]);
                sy.Points.AddXY("通道3", matrix[2, _sy]);
                sy.Points.AddXY("通道4", matrix[3, _sy]);
                chart_synergyMatrix.Series.Add(sy);
            }
        }

        public void plot_activationFactor(double[,] matrix, int sy_num)
        {
            chart_activationFactor.Series.Clear();
            for (int _sy = 0; _sy < sy_num; _sy++)
            {
                Series sy = new Series("协同" + (_sy + 1).ToString());
                sy.ChartType = SeriesChartType.Spline;
                for (int i = 1; i <= matrix.GetLength(1); i+=100)
                {
                    sy.Points.AddXY(i, matrix[_sy, i]);
                }
                chart_activationFactor.Series.Add(sy);
            }
            chart_activation.ChartAreas[0].AxisX.MajorGrid.Interval = 1;
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            string playName = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 14)
                        + "\\videos\\action1_demo.mp4";
            axWindowsMediaPlayer1.Visible = true;
            axWindowsMediaPlayer1.URL = playName;
            axWindowsMediaPlayer1.Ctlcontrols.play();
            timer_video.Start();
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            string playName = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 14)
                        + "\\videos\\action2_demo.mp4";
            axWindowsMediaPlayer1.Visible = true;
            axWindowsMediaPlayer1.URL = playName;
            axWindowsMediaPlayer1.Ctlcontrols.play();
            timer_video.Start();

        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            string playName = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 14)
                        + "\\videos\\action3_demo.mp4";
            axWindowsMediaPlayer1.Visible = true;
            axWindowsMediaPlayer1.URL = playName;
            axWindowsMediaPlayer1.Ctlcontrols.play();
            timer_video.Start();
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            string playName = Directory.GetCurrentDirectory().Substring(0, Directory.GetCurrentDirectory().Length - 14)
                        + "\\videos\\action4_demo.mp4";
            axWindowsMediaPlayer1.Visible = true;
            axWindowsMediaPlayer1.URL = playName;
            axWindowsMediaPlayer1.Ctlcontrols.play();
            timer_video.Start();
        }

        private void timer_video_Tick(object sender, EventArgs e)
        {
            if (this.axWindowsMediaPlayer1.playState.ToString() == "wmppsStopped" || this.axWindowsMediaPlayer1.playState.ToString() == "wmppsReady")
            {
                timer_video.Stop();
                axWindowsMediaPlayer1.Visible = false;
            }
        }

        //记录实验时的肌电采样信号
        private void recordSampleData(short[] buffer) //单关节
        {
            for (int j = 0; j < 8; j++)//8个肌电通道
            {
                //EMGTrainData[counter_trainSamples, j] = (float)buffer[j]*1.43f;
                //EMSampleData[counter_collectSamples, j] = buffer[j] * 1.43;
            }
            counter_collectSamples++;
            if (counter_collectSamples == 90000) // 90s
            {
                timer_plotData.Stop();
                flag_recording = false;
                waveformPlot1.ClearData();
                waveformPlot2.ClearData();
                waveformPlot3.ClearData();
                waveformPlot4.ClearData();
                waveformPlot5.ClearData();
                waveformPlot6.ClearData();
                waveformPlot7.ClearData();
                waveformPlot8.ClearData();
            }
        }

        int t_second, total_second = 90;
        private void timer_collect_Tick(object sender, EventArgs e)
        {
            t_second = total_second - counter_collectSamples / 1000;
            label_indicator.Text = "采集时间剩余：" + t_second.ToString() + "s";
            if (t_second == 0)
            {
                timer_collect.Stop();
            }
        }

        private void saveEMGButton_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                //byte[] data = new byte[10] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                //serialPort1.Write(data, 0, data.Length);
                waveformPlot1.ClearData();
                waveformPlot2.ClearData();
                waveformPlot3.ClearData();
                waveformPlot4.ClearData();
                waveformPlot5.ClearData();
                waveformPlot6.ClearData();
                waveformPlot7.ClearData();
                waveformPlot8.ClearData();
                serialPort1.DiscardInBuffer();
                counter_receiveBytes = 0;
                counter_collectSamples = 0;
            }
            label_indicator.Visible = true;
            timer_collect.Start();
        }
    }
}
