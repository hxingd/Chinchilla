﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.Windows.Threading;
using System.Threading;


namespace Chinchilla {
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class Window2 : Window {
        private Dictionary<string, string> selectedPackage = new Dictionary<string, string>();
        Dictionary<string, string> pkginfo = new Dictionary<string, string>();
        List<Basechart> testchart = new List<Basechart>();
        public List<string> threValue = new List<string>();
        //private DispatcherTimer timerSine;
        public delegate void DeleFunc();
        private ThresholdSetting thresholdSettingWindow;

        public Window2() {
            InitializeComponent();
        }
        
        private void checkupdate() {
            UpdateManager um = new UpdateManager(this);
            um.autoupdate();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            ThreadStart tss = new ThreadStart(checkupdate);
            Thread myThread = new Thread(tss);
            myThread.Start();
            
            initTextBox();
            //updateListview();
            ThreadStart ts = new ThreadStart(InitEnv);
            Thread newThread = new Thread(ts);
            newThread.Start();

            updateStausBar();
            updateLabel();
            testchart.Add(new DatausageChart(Dispatcher, this.chart_datausage, selectedPackage));
            testchart.Add(new MemChart(Dispatcher, this.chart_mem, selectedPackage));
            testchart.Add(new CpuChart(Dispatcher, this.chart_cpu, selectedPackage));
            testchart.Add(new KpiChart(Dispatcher, this.chart_kpi, selectedPackage));
            testchart.Add(new FreeMemChart(Dispatcher, this.chart_freemem, selectedPackage));
            testchart.Add(new FpsChart(Dispatcher, this.chart_fps, selectedPackage));
            grid_kpi.Visibility = System.Windows.Visibility.Collapsed;
            grid_fps.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void InitAdbEnv() {
            Executecmd.ExecuteCommandSync("adb wait-for-device", 0);
            Executecmd.ExecuteCommandSync("adb root", 0);
            Executecmd.ExecuteCommandSync("adb wait-for-device", 0);
            Executecmd.ExecuteCommandSync("adb remount", 0);
            Executecmd.ExecuteCommandSync("adb push save /data/local/save", 0);
            Executecmd.ExecuteCommandSync("adb shell chmod 777 /data/local/save", 0); 
            Executecmd.ExecuteCommandSync("adb push save_fps /data/local/save_fps", 0);
            Executecmd.ExecuteCommandSync("adb shell chmod 777 /data/local/save_fps", 0);
        }

        void initTextBox() {
            this.textBox1.Text = "输入包名搜索";
            this.textBox1.Foreground = System.Windows.Media.Brushes.Gray;
            //this.textBox1.DataContextChanged += new DependencyPropertyChangedEventHandler(textBox1_DataContextChanged);
        }

        void textBox1_TextChanged(object sender, TextChangedEventArgs e) {
            this.listView1.Items.Clear();
            foreach (KeyValuePair<string, string> pkg in pkginfo) {
                if (pkg.Key.IndexOf(this.textBox1.Text) >= 0) {
                    this.listView1.Items.Add(pkg.Key);
                }
            }
        }

        void InitEnv() {
            InitAdbEnv();
            while (listView1.Items.Count == 0) {
                try {
                    pkginfo = DeviceInfoHelper.GetPackageInfo();
                    Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal,
                                                         new DeleFunc(updateListviewDelegate));
                    Thread.Sleep(2000);
                } catch (System.Exception ex) {
                }
            }
        }

        void updateListviewDelegate() {
            foreach (KeyValuePair<string, string> pkg in pkginfo) {
                this.listView1.Items.Add(pkg.Key);
            }
        }


        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (listView1.SelectedItems.Count > 5) {
                listView1.SelectedItems.RemoveAt(5);
                MessageBox.Show("最多选择5个包监测", "提示");
            }

            selectedPackage.Clear();
            foreach (var item in this.listView1.SelectedItems) {
                selectedPackage[item.ToString()] = pkginfo[item.ToString()];
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            if (selectedPackage.Count == 0&&
                (checkBox1.IsChecked == true || 
                checkBox2.IsChecked == true || 
                checkBox3.IsChecked == true)) {
                MessageBox.Show("至少选择1个包监测", "提示");
            } else if (selectedPackage.Count > 5) {
                MessageBox.Show("最多选择5个包监测", "提示");
            } else {
                foreach (Basechart chart in testchart) {
                    chart.updatechart(selectedPackage);
                }
                //testchart.Clear();          
            }
        }

        private void updateStausBar() {
            DispatcherTimer timerSine = new DispatcherTimer();
            timerSine.Tick += new EventHandler(updateStatus);
            timerSine.Interval = new TimeSpan(0, 0, 1);
            timerSine.Start();

        }

        private void updateStatus(object sender, EventArgs e) {
            if (listView1.SelectedItems.Count > 1) {
                return;
            }
            string selectedProc = "";
            foreach (var pkginfo in this.selectedPackage) {
                if (selectedProc != "")
                    selectedProc = selectedProc + ";";
                selectedProc = selectedProc + pkginfo.Key;
            }
            this.status_proname.Content = selectedProc;
            if (this.testchart.Count > 0) {
                this.status_datausage.Content = "流量:" + this.testchart[0].CurrentData.ToString("f2") + "KB";
                this.status_cpu.Content = "CPU:" + this.testchart[2].CurrentData + "%";
                this.status_mem.Content = "内存:" + this.testchart[1].CurrentData.ToString("f2") + "MB";
                this.status_fps.Content = "FPS:" + this.testchart[5].CurrentData.ToString();
            }
            if (this.threValue.Count > 0) {
                this.status_datausage_thre.Content = "阈值:" + this.threValue[0] + "KB";
                this.status_cpu_thre.Content = "阈值:" + this.threValue[1] + "%";
                this.status_mem_thre.Content = "阈值:" + this.threValue[2] + "MB";

                if (this.testchart.Count > 0) {
                    if (this.testchart[0].CurrentData > Convert.ToDouble(this.threValue[0])) {
                        this.status_datausage.Background = new SolidColorBrush(Colors.Red);
                    }

                    if (this.testchart[1].CurrentData > Convert.ToDouble(this.threValue[1])) {
                        this.status_cpu.Background = new SolidColorBrush(Colors.Red);
                    }

                    if (this.testchart[2].CurrentData > Convert.ToDouble(this.threValue[2])) {
                        this.status_mem.Background = new SolidColorBrush(Colors.Red);
                    }
                }
            }

        }

        private void updateLabel() {
            DispatcherTimer timerSine = new DispatcherTimer();
            timerSine.Tick += new EventHandler(updateLabelText);
            timerSine.Interval = new TimeSpan(0, 0, 5);
            timerSine.Start();

        }

        private void updateLabelText(object sender, EventArgs e) {
            if (listView1.SelectedItems.Count > 1) {
                return;
            }
            if (this.testchart.Count > 0) {
                this.label_cpu.Content = "平均值:" + ((CpuChart)this.testchart[2]).AvgData.ToString("0") + "%";
                this.label_mem.Content = "平均值:" + ((MemChart)this.testchart[1]).AvgData.ToString("0.000") + "MB";

                this.label_cpu_max.Content = "最大值:" + ((CpuChart)this.testchart[2]).MaxData.ToString("0") + "%";
                this.label_mem_max.Content = "最大值:" + ((MemChart)this.testchart[1]).MaxData.ToString("0.000") + "MB";
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e) {
            if (this.thresholdSettingWindow == null) {
                this.thresholdSettingWindow = new ThresholdSetting();
                this.thresholdSettingWindow.Owner = this;
                this.thresholdSettingWindow.Show();
            } else {
                this.thresholdSettingWindow.Show();
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e) {
            MessageBox.Show("1.右键点击图标可保存图片及查看更多操作，滚轮可放大/缩小图片\n 2.目前阈值报警功能仅支持监测一个应用时使用\n3.beta版功能较少，有任何需求请联系邓呈亮&&何韡", "提示");
        }

        private void Window_Closed(object sender, EventArgs e) {
            foreach (Basechart chart in testchart) {
                chart.dispose();
            }
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e) {
            if (((CheckBox)sender).IsChecked == true) {
                if (sender.ToString().Contains("流量")) {
                    testchart[0].restart();
                    grid_datausage.Visibility = System.Windows.Visibility.Visible;
                } else if (sender.ToString().Contains("空闲内存")) {
                    testchart[4].restart();
                    grid_freemem.Visibility = System.Windows.Visibility.Visible;
                } else if (sender.ToString().Contains("内存")) {
                    testchart[1].restart();
                    grid_mem.Visibility = System.Windows.Visibility.Visible;
                } else if (sender.ToString().Contains("CPU")) {
                    testchart[2].restart();
                    grid_cpu.Visibility = System.Windows.Visibility.Visible;
                } else if (sender.ToString().Contains("KPI")) {
                    ((KpiChart)testchart[3]).restart();
                    grid_kpi.Visibility = System.Windows.Visibility.Visible;
                } else if (sender.ToString().Contains("FPS")) {
                    ((FpsChart)testchart[5]).restart();
                    grid_fps.Visibility = System.Windows.Visibility.Visible;
                } else {
                }
            } else if (((CheckBox)sender).IsChecked == false) {
                if (sender.ToString().Contains("流量")) {
                    testchart[0].stop();
                    grid_datausage.Visibility = System.Windows.Visibility.Collapsed;
                } else if (sender.ToString().Contains("空闲内存")) {
                    testchart[4].stop();
                    grid_freemem.Visibility = System.Windows.Visibility.Collapsed;
                } else if (sender.ToString().Contains("内存")) {
                    grid_mem.Visibility = System.Windows.Visibility.Collapsed;
                    testchart[1].stop();
                } else if (sender.ToString().Contains("CPU")) {
                    testchart[2].stop();
                    grid_cpu.Visibility = System.Windows.Visibility.Collapsed;
                } else if (sender.ToString().Contains("KPI")) {
                    ((KpiChart)testchart[3]).stop();
                    grid_kpi.Visibility = System.Windows.Visibility.Collapsed;
                } else if (sender.ToString().Contains("KPI")) {
                    ((KpiChart)testchart[3]).stop();
                    grid_kpi.Visibility = System.Windows.Visibility.Collapsed;
                } else if (sender.ToString().Contains("FPS")) {
                    ((FpsChart)testchart[5]).stop();
                    grid_fps.Visibility = System.Windows.Visibility.Collapsed;
                } else {
                }
            }
        }

        private void cleanTooltips(object sender, KeyboardFocusChangedEventArgs e) {
            if (this.textBox1.Text.Equals("输入包名搜索")) {
                this.textBox1.Text = "";
                this.textBox1.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        //show packagelist

        private void pacakgedock_MouseLeave(object sender, MouseEventArgs e) {
            pacakgedock.Visibility = System.Windows.Visibility.Collapsed;
            dockPanel2.Visibility = System.Windows.Visibility.Visible;
        }

        private void dockPanel2_MouseEnter(object sender, MouseEventArgs e) {
            pacakgedock.Visibility = System.Windows.Visibility.Visible;
            dockPanel2.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}