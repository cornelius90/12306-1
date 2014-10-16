﻿using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using JasonLong.Helper;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Newtonsoft.Json.Linq;
using TrainAssistant.Models;
using System.Web;

namespace TrainAssistant
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        TicketHelper ticketHelper = new TicketHelper();
        public const string accountFile = "Account";//登录用户信息
        string seatTypes = "";//席别

        public MainWindow()
        {
            InitializeComponent();
        }

        //窗口加载
        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {

            txtDate.DisplayDateStart = DateTime.Now;
            txtDate.DisplayDateEnd = DateTime.Now.AddDays(19);
            txtDate.Text = txtDate.DisplayDateEnd.Value.ToString("yyyy-MM-dd");
            chkDayBefore.Content = DateTime.Parse(txtDate.Text).AddDays(-1).ToString("yyyy-MM-dd");
            chkTwoDayBefore.Content = DateTime.Parse(txtDate.Text).AddDays(-2).ToString("yyyy-MM-dd");

            IsShowLoginPopup(true);

            byte[] buffter = TrainAssistant.Properties.Resources.data;
            if (!BasicOCR.LoadLibFromBuffer(buffter, buffter.Length, "123"))
            {
                MessageBox.Show("API初始化失败！");
            }

        }

        /// <summary>
        /// 是否显示登录界面
        /// </summary>
        /// <param name="isShow"></param>
        public async void IsShowLoginPopup(bool isShow)
        {
            if (isShow)
            {
                btnLogout.Visibility = Visibility.Hidden;
                gridOpacity.Visibility = Visibility.Visible;
                loginPopup.Visibility = Visibility.Visible;
                lblErrorMsg.Content = "";

                await GetValidateCodeImage();

                List<Users> users = ticketHelper.ReadUser(accountFile);
                if (users.Count > 0)
                {
                    txtUserName.ItemsSource = users;
                    txtUserName.DisplayMemberPath = "Name";
                    txtUserName.SelectedValuePath = "Name";
                    txtUserName.SelectedIndex = 0;
                }
            }
            else
            {
                btnLogout.Visibility = Visibility.Visible;
                gridOpacity.Visibility = Visibility.Hidden;
                loginPopup.Visibility = Visibility.Hidden;
            }
        }

        //注册新用户
        private void linkJoin_Click(object sender, RoutedEventArgs e)
        {
            var reg_url = ConfigurationManager.AppSettings["UserRegisterUrl"].ToString();
            Process.Start(reg_url);
        }

        //忘记密码
        private void linkForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            var forgot_url = ConfigurationManager.AppSettings["ForgotPasswordUrl"].ToString();
            Process.Start(forgot_url);
        }

        //自动登录选项
        private void chkAutoLogin_Click(object sender, RoutedEventArgs e)
        {
            if (chkAutoLogin.IsChecked == true)
            {
                chkRemeberMe.IsChecked = true;
            }
        }

        //更改用户登录
        private void txtUserName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<Users> users = ticketHelper.ReadUser(accountFile);
            if (users.Count > 0 && txtUserName.SelectedIndex > -1)
            {
                var model = (from m in users
                             where m.Name == txtUserName.SelectedValue.ToString()
                             select m).FirstOrDefault<Users>();
                if (txtUserName.SelectedValue.ToString() == model.Name)
                {
                    txtPassword.Password = model.Password;
                    chkRemeberMe.IsChecked = true;
                    if (model.IsAutoLogin)
                    {
                        chkAutoLogin.IsChecked = true;
                    }
                    else
                    {
                        chkAutoLogin.IsChecked = false;
                    }
                }
            }
            else
            {
                txtPassword.Password = "";
                chkRemeberMe.IsChecked = false;
                chkAutoLogin.IsChecked = false;
            }
        }

        /// <summary>
        /// 刷新登录验证码
        /// </summary>
        /// <returns></returns>
        private async Task GetValidateCodeImage()
        {
            progressRingAnima.IsActive = true;
            lblStatusMsg.Content = "获取验证码中...";
            Dictionary<BitmapImage, string> dicLoginCode = await ticketHelper.GetLoginCodeAsync();
            imgCode.Source = dicLoginCode.Keys.First();
            txtValidateCode.Text = dicLoginCode.Values.First();
            lblStatusMsg.Content = "获取验证码完成";
            progressRingAnima.IsActive = false;
        }

        /// <summary>
        /// 刷新订单验证码
        /// </summary>
        private async Task GetOrderCode()
        {
            progressRingAnima.IsActive = true;
            Dictionary<BitmapImage, string> dicOrderCode = await ticketHelper.GetSubmitOrderCode();
            imgOrderCode.Source = dicOrderCode.Keys.First();
            txtOrderCode.Text = dicOrderCode.Values.First();
            progressRingAnima.IsActive = false;
        }

        //更换验证码
        private async void imgCode_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            await GetValidateCodeImage();
        }

        //登录
        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (txtUserName.Text.Trim() == "")
            {
                lblErrorMsg.Content = "用户名不能为空";
                return;
            }
            if (txtPassword.Password.Trim() == "")
            {
                lblErrorMsg.Content = "密码不能为空";
                return;
            }
            if (txtValidateCode.Text.Trim() == "")
            {
                lblErrorMsg.Content = "验证码不能为空";
                return;
            }
            if (txtValidateCode.Text.Length > 5 || txtValidateCode.Text.Length < 4)
            {
                lblErrorMsg.Content = "验证码长度为4位";
                return;
            }
            progressRingAnima.IsActive = true;
            lblStatusMsg.Content = "正在登录...";
            btnLogin.IsEnabled = false;
            try
            {
                string loginCodeResult = await ticketHelper.ValidateLoginCode(txtValidateCode.Text.Trim());
                if (loginCodeResult.Contains("验证码错误"))
                {
                    return;
                }
                lblErrorMsg.Content = await ticketHelper.Login(txtUserName.Text.Trim(), txtPassword.Password.Trim(), txtValidateCode.Text.Trim(), (bool)chkRemeberMe.IsChecked, (bool)chkAutoLogin.IsChecked);
            }
            catch (Exception)
            {
                lblErrorMsg.Content = "未知错误";
                lblStatusMsg.Content = "登录失败";
                btnLogin.IsEnabled = true;
                progressRingAnima.IsActive = false;
            }
            string loginName = lblErrorMsg.Content.ToString();
            if (loginName.IndexOf("登录成功") > 0)
            {
                IsShowLoginPopup(false);
                lblLoginName.Text = "欢迎，" + loginName.Substring(0, loginName.IndexOf("登录成功"));
                lblStatusMsg.Content = loginName.Substring(loginName.IndexOf("登录成功"));
            }
            else
            {
                await GetValidateCodeImage();
            }
            btnLogin.IsEnabled = true;
            progressRingAnima.IsActive = false;
        }

        //退出登录
        private async void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await ticketHelper.Logout();
                if (result == "登录")
                {
                    lblLoginName.Text = "登录";
                    IsShowLoginPopup(true);
                }
            }
            catch (Exception)
            {
                lblStatusMsg.Content = "注销异常";
            }
        }

        //出发站
        private async void txtStartCity_KeyUp(object sender, KeyEventArgs e)
        {
            if (txtStartCity.Text.Trim() != "")
            {
                var list = await ticketHelper.GetCitys(txtStartCity.Text.ToLower());
                if (list.Count > 0)
                {
                    txtStartCity.ItemsSource = list;
                    txtStartCity.DisplayMemberPath = "ZHName";
                    txtStartCity.SelectedValuePath = "Code";
                    txtStartCity.IsDropDownOpen = true;
                }
            }
        }

        //到达站
        private async void txtEndCity_KeyUp(object sender, KeyEventArgs e)
        {
            if (txtEndCity.Text.Trim() != "")
            {
                var list = await ticketHelper.GetCitys(txtEndCity.Text.ToLower());
                if (list.Count > 0)
                {
                    txtEndCity.ItemsSource = list;
                    txtEndCity.DisplayMemberPath = "ZHName";
                    txtEndCity.SelectedValuePath = "Code";
                    txtEndCity.IsDropDownOpen = true;
                }
            }
        }

        /// <summary>
        /// 绑定联系人
        /// </summary>
        /// <returns></returns>
        private async Task BindContact(bool isSearch)
        {
            progressRingAnima.IsActive = true;
            if (isSearch)
            {
                await ticketHelper.SaveContacts(txtContactName.Text.Trim());
            }
            List<Contacts> contacts = ticketHelper.ReadContacts("Contact");
            if (!isSearch)
            {
                contacts = (from c in contacts
                            where c.PassengerName.Contains(txtContactName.Text.Trim())
                            select c).ToList<Contacts>();
            }
            gridContact.ItemsSource = contacts;
            lblTicketCount.Content = "共" + contacts.Count() + "个联系人";
            progressRingAnima.IsActive = false;
        }

        /// <summary>
        /// 根据车次获取席别
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private Task<Dictionary<string, string>> GetTickType(string type)
        {
            return Task.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                Dictionary<string, string> tickType = new Dictionary<string, string>();
                if (File.Exists("SeatType.txt"))
                {
                    using (StreamReader read = new StreamReader("SeatType.txt", Encoding.UTF8))
                    {
                        string result = read.ReadToEnd();
                        if (result != "")
                        {
                            JObject json = JObject.Parse(result);
                            var t = (from j in json[type]
                                     select new { value = j["value"].ToString(), id = j["id"].ToString() }).ToList();
                            tickType = t.ToDictionary(c => c.id, c => c.value);
                        }
                    }
                }
                return tickType;
            });
        }

        /// <summary>
        /// 获取列车类型
        /// </summary>
        /// <returns></returns>
        private string GetCheckedTickType()
        {
            string result = "";
            CheckBox chkTickType;
            foreach (Control type in gridTicketType.Children)
            {
                if (type is CheckBox)
                {
                    chkTickType = type as CheckBox;
                    if (chkTickType.IsChecked == true)
                    {
                        result += chkTickType.Tag + "#";
                    }
                }
            }
            return result;
        }

        //登录按钮
        private void btnLoginPopup_Click(object sender, RoutedEventArgs e)
        {
            if (lblLoginName.Text != "登录")
            {
                IsShowLoginPopup(false);
            }
            else
            {
                IsShowLoginPopup(true);
            }
        }


        //展开日期
        private void txtDate_MouseUp(object sender, MouseButtonEventArgs e)
        {
            txtDate.IsDropDownOpen = true;
        }

        //选择全部车类
        private void chkAll_Click(object sender, RoutedEventArgs e)
        {
            if (chkAll.IsChecked == true)
            {
                chkEMU.IsChecked = chkK.IsChecked = chkZ.IsChecked = chkT.IsChecked = chkOther.IsChecked = chkHSR.IsChecked = true;
            }
            else
            {
                chkEMU.IsChecked = chkK.IsChecked = chkZ.IsChecked = chkT.IsChecked = chkOther.IsChecked = chkHSR.IsChecked = false;
            }
        }

        //选择高铁
        private void chkHSR_Click(object sender, RoutedEventArgs e)
        {
            if (chkHSR.IsChecked == true && chkEMU.IsChecked == true && chkK.IsChecked == true && chkZ.IsChecked == true && chkT.IsChecked == true && chkOther.IsChecked == true)
            {
                chkAll.IsChecked = true;
            }
            else
            {
                chkAll.IsChecked = false;
            }
        }

        //选择动车
        private void chkEMU_Click(object sender, RoutedEventArgs e)
        {
            if (chkEMU.IsChecked == true && chkK.IsChecked == true && chkZ.IsChecked == true && chkT.IsChecked == true && chkOther.IsChecked == true)
            {
                chkAll.IsChecked = true;
            }
            else
            {
                chkAll.IsChecked = false;
            }
        }

        //选择最快
        private void chkZ_Click(object sender, RoutedEventArgs e)
        {
            if (chkZ.IsChecked == true && chkEMU.IsChecked == true && chkK.IsChecked == true && chkT.IsChecked == true && chkOther.IsChecked == true)
            {
                chkAll.IsChecked = true;
            }
            else
            {
                chkAll.IsChecked = false;
            }
        }

        //选择特快
        private void chkT_Click(object sender, RoutedEventArgs e)
        {
            if (chkT.IsChecked == true && chkEMU.IsChecked == true && chkK.IsChecked == true && chkZ.IsChecked == true && chkOther.IsChecked == true)
            {
                chkAll.IsChecked = true;
            }
            else
            {
                chkAll.IsChecked = false;
            }
        }

        //选择K字头
        private void chkK_Click(object sender, RoutedEventArgs e)
        {
            if (chkK.IsChecked == true && chkEMU.IsChecked == true && chkZ.IsChecked == true && chkT.IsChecked == true && chkOther.IsChecked == true)
            {
                chkAll.IsChecked = true;
            }
            else
            {
                chkAll.IsChecked = false;
            }
        }

        //选择其他
        private void chkOther_Click(object sender, RoutedEventArgs e)
        {
            if (chkOther.IsChecked == true && chkEMU.IsChecked == true && chkK.IsChecked == true && chkZ.IsChecked == true && chkT.IsChecked == true == true)
            {
                chkAll.IsChecked = true;
            }
            else
            {
                chkAll.IsChecked = false;
            }
        }

        //切换地址
        private void btnChangeAddress_Click(object sender, RoutedEventArgs e)
        {
            string startStation = txtStartCity.Text.ToString();
            string endStation = txtEndCity.Text.ToString();

            txtStartCity.Text = endStation;
            txtEndCity.Text = startStation;
        }

        //自动搜索
        private void chkAutoSearch_Click(object sender, RoutedEventArgs e)
        {
            if (chkAutoSearch.IsChecked == true)
            {

            }
        }

        //搜索
        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (txtStartCity.Text.Trim() == "")
            {
                lblStatusMsg.Content = "出发地不能为空";
                return;
            }
            if (txtEndCity.Text.Trim() == "")
            {
                lblStatusMsg.Content = "目的地不能为空";
                return;
            }
            if (txtDate.Text.Trim() == "")
            {
                lblStatusMsg.Content = "出发日期不能为空";
                return;
            }
            string isNormal = rdoNormal.IsChecked == true ? rdoNormal.Tag.ToString() : rdoStudent.Tag.ToString();
            progressRingAnima.IsActive = true;
            List<Tickets> ticketModel = await ticketHelper.GetSearchTrain(txtDate.Text.Replace('/', '-'), txtStartCity.SelectedValue.ToString(), txtEndCity.SelectedValue.ToString(), isNormal);
            gridTrainList.ItemsSource = ticketModel;
            lblTicketCount.Content = txtStartCity.Text + "-->" + txtEndCity.Text + "（共" + ticketModel.Count() + "趟列车）";
            progressRingAnima.IsActive = false;
        }

        //预订
        private async void Reservate_Click(object sender, RoutedEventArgs e)
        {
            Tickets tickets = gridTrainList.SelectedItem as Tickets;
            seatTypes = "";
            for (int o = 4; o < gridTrainList.Columns.Count - 1; o++)
            {
                string str = (gridTrainList.Columns[o].GetCellContent(gridTrainList.SelectedItem) as TextBlock).Text;
                if (!str.Contains("--") && !str.Contains("无"))
                {
                    seatTypes += gridTrainList.Columns[o].Header.ToString() + "@";
                }
            }
            lblTicket.Content = tickets.FromStationName + "→" + tickets.ToStationName + "(" + tickets.TrainName + ")";
            lblTicket.Tag = tickets.StartTrainDate + "," + tickets.TrainNo + "," + tickets.TrainName + "," + tickets.FromStationCode + "," + tickets.ToStationCode + "," + tickets.YPInfo + "," + tickets.LocationCode;
            string purposeCode = rdoNormal.IsChecked == true ? rdoNormal.Tag.ToString() : rdoStudent.Tag.ToString();
            Dictionary<bool, string> dicSubmitOrderReq = await ticketHelper.SubmitOrderRequest(tickets, purposeCode);
            if (!dicSubmitOrderReq.Keys.First())
            {
                MessageBox.Show(dicSubmitOrderReq.Values.First(), "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            progressRingAnima.IsActive = true;
            gridOpacity.Visibility = Visibility.Visible;
            orderPopup.Visibility = Visibility.Visible;
            List<Contacts> contacts = ticketHelper.ReadContacts("Contact");
            int row = contacts.Count;//(int)Math.Ceiling((double)contacts.Count / 3);
            while (row-- > 0)
            {
                gContacts.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength()
                });
            }
            if (contacts.Count > 0)
            {
                gContacts.Children.Clear();
                for (int i = 0; i < contacts.Count; i++)
                {
                    CheckBox chkContact = new CheckBox()
                    {
                        Content = contacts[i].PassengerName,
                        Name = "chk" + contacts[i].Code,
                        Height = 15,
                        Tag = contacts[i].PassengerTypeName + "#" + contacts[i].PassengerName + "#" + contacts[i].PassengerIdTypeName + "#" + contacts[i].PassengerIdNo + "#" + contacts[i].Mobile
                    };
                    chkContact.Click += chkContact_Click;
                    gContacts.Children.Add(chkContact);
                    chkContact.SetValue(Grid.RowProperty, i);
                    chkContact.SetValue(Grid.ColumnProperty, 0);
                }
            }
            lblSecretStr.Content = await ticketHelper.GetSubmitOrderToken();
            await GetOrderCode();
            progressRingAnima.IsActive = false;
        }

        //选择乘客
        async void chkContact_Click(object sender, RoutedEventArgs e)
        {
            string val = "";
            CheckBox chk;
            foreach (var c in gContacts.Children)
            {
                if (c is CheckBox)
                {
                    chk = c as CheckBox;
                    if (chk.IsChecked == true)
                    {
                        val += chk.Tag + ",";
                    }
                }
            }
            string train = lblTicket.Content.ToString().Substring(lblTicket.Content.ToString().IndexOf("(") + 1, 1);
            if (train == "G")
            {
                train = "D";
            }
            Dictionary<string, string> seatType = await GetTickType(train);//席别
            IDictionary<string, string> seat_Type = new Dictionary<string, string>();
            var type = seatTypes.Split('@');
            for (int i = 0; i < type.Count(); i++)
            {
                if (type[i] != "")
                {
                    var _seatType = (from s in seatType
                                     where s.Value == type[i]
                                     select s).ToDictionary(t => t.Key, t => t.Value);
                    foreach (var t in _seatType)
                    {
                        if (!seat_Type.Contains(t))
                        {
                            seat_Type.Add(t);
                        }
                    }
                }
            }

            Dictionary<string, string> tickType = new Dictionary<string, string>();//票种
            tickType.Add("成人票", "1");
            tickType.Add("儿童票", "2");
            tickType.Add("学生票", "3");
            tickType.Add("残军票", "4");
            Dictionary<string, string> idType = new Dictionary<string, string>();//证件类型
            idType.Add("二代身份证", "1");
            idType.Add("一代身份证", "2");
            idType.Add("港澳通行证", "C");
            idType.Add("台湾通行证", "G");
            idType.Add("护照", "B");

            List<SubmitOrder> subOrder = new List<SubmitOrder>();
            if (val != "")
            {
                var selPassenger = val.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);//选中的乘客
                if (selPassenger.Count() > 5)
                {
                    MessageBox.Show("乘客数不能超过5个", "消息", MessageBoxButton.OK, MessageBoxImage.Warning);
                    CheckBox chkItem = e.Source as CheckBox;
                    chkItem.IsChecked = false;
                    return;
                }
                for (int j = 0; j < selPassenger.Count(); j++)
                {
                    if (selPassenger[j] != "")
                    {
                        var item = selPassenger[j].Split('#');
                        SubmitOrder subOrderModel = new SubmitOrder();
                        subOrderModel.SeatType = seat_Type.ToDictionary(m => m.Key, m => m.Value);
                        subOrderModel.SelSeatType = subOrderModel.SeatType.Keys.Last();
                        subOrderModel.TickType = (from t in tickType
                                                  where t.Key.Contains(item[0])
                                                  select t).ToDictionary(m => m.Key, m => m.Value);
                        subOrderModel.SelTickType = subOrderModel.TickType.Values.First();
                        subOrderModel.PassengerName = item[1];
                        subOrderModel.IDType = (from d in idType
                                                where d.Key.Contains(item[2])
                                                select d).ToDictionary(m => m.Key, m => m.Value);
                        subOrderModel.SelIDType = subOrderModel.IDType.Values.First();
                        subOrderModel.PassengerId = item[3];
                        subOrderModel.PassengerMobile = item[4];
                        subOrder.Add(subOrderModel);
                    }
                }
            }

            gridPassenger.ItemsSource = subOrder;
        }

        //加载常用联系人
        private async void tabContact_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            await BindContact(true);
        }

        //查询联系人
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await BindContact(false);
        }

        //关闭提交订单层
        private void btnClosePopup_Click(object sender, RoutedEventArgs e)
        {
            gridOpacity.Visibility = Visibility.Hidden;
            orderPopup.Visibility = Visibility.Hidden;
            gridPassenger.ItemsSource = null;
        }

        //更换订单验证码
        private async void imgOrderCode_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            await GetOrderCode();
        }

        //提交
        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (gridPassenger.Items.Count < 1)
            {
                lblStatusMsg.Content = "未选择乘客";
                return;
            }
            if (txtOrderCode.Text.Trim() == "")
            {
                lblStatusMsg.Content = "验证码不能为空";
                return;
            }
            if (txtOrderCode.Text.Trim().Length < 4 || txtOrderCode.Text.Trim().Length > 4)
            {
                lblStatusMsg.Content = "验证码长度为4位";
                return;
            }
            var lstPassengers = gridPassenger.ItemsSource as List<SubmitOrder>;
            progressRingAnima.IsActive = true;
            List<string> lstQueues = new List<string>();
            var arrQueues = lblTicket.Tag.ToString().Split(',');
            for (int i = 0; i < arrQueues.Length; i++)
            {
                lstQueues.Add(arrQueues[i]);
            }
            var arrToken = lblSecretStr.Content.ToString().Split(',');
            string orderResult = await SubmitOrder(lstPassengers, txtOrderCode.Text, arrToken[0], lstQueues, arrToken[1]);
            lblStatusMsg.Content = orderResult;
            MessageBox.Show(orderResult, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            progressRingAnima.IsActive = false;
        }

        /// <summary>
        /// 提交订单
        /// </summary>
        /// <returns></returns>
        private async Task<string> SubmitOrder(List<SubmitOrder> lstPassengers, string submitOrderCode, string token, List<string> lstQueues, string keyChange)
        {
            string oldPassengers = "", passengerTickets = "";
            int p = 0;
            foreach (var passenger in lstPassengers)
            {
                //证件类型
                DataGridCell selIDTypeCell = await ticketHelper.GetDataGridCell(gridPassenger, p, 4);
                var idTypeValue = selIDTypeCell.Content as ComboBox;
                //席别
                DataGridCell selSeatTypeCell = await ticketHelper.GetDataGridCell(gridPassenger, p, 0);
                var seateTypeValue = selSeatTypeCell.Content as ComboBox;
                //票种
                DataGridCell selTickTypeCell = await ticketHelper.GetDataGridCell(gridPassenger, p, 3);
                var tickTypeValue = selTickTypeCell.Content as ComboBox;

                oldPassengers += passenger.PassengerName + "," + idTypeValue.SelectedValue + "," + passenger.PassengerId + "," + tickTypeValue.SelectedValue + "_";
                passengerTickets += seateTypeValue.SelectedValue + ",0," + tickTypeValue.SelectedValue + "," + passenger.PassengerName + "," + idTypeValue.SelectedValue + "," + passenger.PassengerId + "," + passenger.PassengerMobile + ",N_";
                p++;
            }
            passengerTickets = HttpUtility.UrlEncode(passengerTickets.TrimEnd('_'));
            oldPassengers = HttpUtility.UrlEncode(oldPassengers);

            //检验验证码
            bool checkOrderCode = await ticketHelper.CheckOrderCode(submitOrderCode, token);
            string resultMsg = "";
            if (!checkOrderCode)
            {
                return "验证码不正确";
            }
            //检查订单信息
            Dictionary<bool, string> checkOrderInfo = await ticketHelper.CheckOrderInfo(passengerTickets, oldPassengers, submitOrderCode, token);
            if (!checkOrderInfo.Keys.First())
            {
                return checkOrderInfo.Values.First();
            }
            //获取排队
            OrderQueue orderQueue = await ticketHelper.GetQueueCount(lstQueues, token, checkOrderInfo.Values.First());
            if (orderQueue.OP_2)
            {
                return "排队人数（" + orderQueue.countT + "）超过余票数。";
            }
            //确认订单
            Dictionary<bool, string> confirmOrder = await ticketHelper.ConfirmOrderQueue(passengerTickets, oldPassengers, submitOrderCode, token, keyChange, lstQueues);
            if (!confirmOrder.Keys.First())
            {
                return confirmOrder.Values.First();
            }
            //O038850 401 M060350 030 P071950 008 -8
            //O038850 401 M060350 030 P071950 008 -30
            //O038850 401 M060350 030 P071950 008 -401

            //等待出票
            while (true)
            {
                QueryOrderWaitTime queryOrderWaitTime = await ticketHelper.QueryOrderWaitTime("", "dc", "", token);
                if (queryOrderWaitTime.WaitTime <= 0)
                {
                    if (!string.IsNullOrEmpty(queryOrderWaitTime.OrderId))
                    {
                       resultMsg="出票成功！订单号：【" + queryOrderWaitTime.OrderId + "】";
                    }
                    break;
                }
                else
                {
                    lblStatusMsg.Content = queryOrderWaitTime.Count > 0 ? "前面还有【" + queryOrderWaitTime.WaitCount + "】订单等待处理，大约需等待【" + queryOrderWaitTime.WaitTime + "】秒" : "正在处理订单，大约需要" + queryOrderWaitTime.WaitTime + "秒";
                }
            }
            return resultMsg;
        }

    }
}
