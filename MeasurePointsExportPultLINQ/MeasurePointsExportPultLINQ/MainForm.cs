using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Lers;
using Lers.Core;
using System.Reflection;
using System.Security;
using Lers.Networking;
using System.Xml.Linq;

namespace MeasurePointsExportPultLINQ
{
    public partial class MainForm : Form
    {
        LersServer lersServer;
        AboutForm aboutForm;
        HelpForm helpForm;
        ChangeServer changeServer;
        private DateTime dateTimeStart, dateTimeEnd;
        private int? measurepointNumber;
        private string _heatLeadIn, _supplyChannel, _returnChannel;
        public string _tbServer, _tbPort;

        private void SelectTbPassword()
        {
            tbPassword.SelectionStart = 0;
            tbPassword.SelectionLength = tbPassword.Text.Length;
        }

        public MainForm()
        {
            InitializeComponent();
            tbLogin.Select();
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.Text = "Экспорт точек учета ЛЭРС Учет в Универсальный пульт версия " + version;
            _tbServer = tbServer.Text;
            _tbPort = tbPort.Text;
        }

        private void LersConnect()
        {
            lersServer = new LersServer();

            string login = tbLogin.Text;
            string secureString = tbPassword.Text;

            SecureString password = new SecureString();
            foreach (char symbol in secureString)
            {
                password.AppendChar(symbol);
            }

            BasicAuthenticationInfo authInfo = new BasicAuthenticationInfo(login, password);

            try
            {
                //Игнорируем разницу в версиях в ЛЭРС Учет
                lersServer.VersionMismatch += (sender, e) => e.Ignore = true;

                lersServer.Connect(tbServer.Text, Convert.ToUInt16(tbPort.Text), authInfo);
            }
            catch (ServerConnectionException connection)
            {
                MessageBox.Show(connection.Message, "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tbLogin.Select();
                return;
            }
            catch (AuthorizationFailedException authorization)
            {
                MessageBox.Show(authorization.Message, "Ошибка входа", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tbLogin.Select();
                return;
            }
            catch (LersServerException server)
            {
                MessageBox.Show(server.Message, "Ошибка обработки запроса сервером", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tbLogin.Select();
                return;
            }
        }

        private void butEnter_Click(object sender, EventArgs e)
        {
            if (tbLogin.Text == "")
            {
                MessageBox.Show("Введите имя пользователя", "Имя пользователя", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tbLogin.Select();
            }
            else if (tbPassword.Text == "")
            {
                MessageBox.Show("Введите пароль", "Пароль", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tbPassword.Select();
            }
            else
            {
                LersConnect();
                if (lersServer.IsConnected)
                {
                    butExport.Enabled = true;
                    butCancel.Enabled = true;
                    butChange.Enabled = false;
                }
                else
                {
                    tbLogin.Text = "";
                    tbPassword.Text = "";
                    butExport.Enabled = false;
                    butChange.Enabled = true;
                }
            }
        }

        private void aboutMenu_Click(object sender, EventArgs e)
        {
            if (aboutForm == null || aboutForm.IsDisposed)
            {
                aboutForm = new AboutForm();
                aboutForm.Owner = this;
                aboutForm.Show();
            }
            else aboutForm.Activate();
        }

        private void exitMenu_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (waitProgressBar.Style == ProgressBarStyle.Marquee)
            {
                DialogResult dialog = MessageBox.Show("Вы уверены, что хотите выйти из программы?\nВсе запущенные операции будут прерваны", "Подтверждение выхода", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialog == DialogResult.No)
                    e.Cancel = true;
            } 
        }

        private void butCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void helpMenu_Click(object sender, EventArgs e)
        {
            if (helpForm == null || helpForm.IsDisposed)
            {
                helpForm = new HelpForm();
                helpForm.Owner = this;
                helpForm.Show();
            }
            else helpForm.Activate();
        }

        private void butChange_Click(object sender, EventArgs e)
        {
            changeServer = new ChangeServer();
            changeServer.Owner = this;
            changeServer.ShowDialog();
        }

        private void tbPassword_Click(object sender, EventArgs e)
        {
            SelectTbPassword();
        }

        private void tbPassword_Enter(object sender, EventArgs e)
        {
            SelectTbPassword();
        }

        private async void butExport_Click(object sender, EventArgs e)
        {
            
            // Блокируем элементы на форме
            waitProgressBar.Style = ProgressBarStyle.Marquee;
            waitProgressBar.MarqueeAnimationSpeed = 20;
            tbServer.Enabled = false;
            tbLogin.Enabled = false;
            tbPassword.Enabled = false;
            butExport.Enabled = false;
            butEnter.Enabled = false;
            butCancel.Enabled = true;

            // Начинаем отсчет вермени
            dateTimeStart = DateTime.Now;

            // Объявляем переменную для выбора директории сохранения XML-файла
            string pathToXmlFile = Environment.CurrentDirectory + "\\" + "UniversalReader-Settings.xml";

            #region Начинаем работу с XML LINQ
            // Объявляем элементы XML 
            XElement _measurepoint, _isDoublePipeHotWaterSystem, _portSpeed, heatLeadIn;

            // Объявляем XML-документ
            XDocument xdoc = new XDocument(
                    new XElement("settings", // Создаем главный элемент "настройки" 
                       new XElement("serialPortName", ""), // Настройка COM-порта
                       new XElement("listenPort", 10000), // Порт ЛЭРС Учета (по умолчанию 10000)
                       new XElement("saveDirectory", "C:\\Program Files\\LERS\\UniversalReader\\LERS") // Директория хранения данных при опросе 
                      ));
            #endregion

            #region Создаем элементы XML-файла
            //Объявляем класс, реализующий интерфейс точки учёта
            MeasurePoint[] measurePointsCollection = lersServer.MeasurePoints.GetList(MeasurePointType.Regular, MeasurePointInfoFlags.Attributes | MeasurePointInfoFlags.Equipment);
            try
            {
                //Выводим все точки учёта из коллекции
                foreach (MeasurePoint points in measurePointsCollection)
                {
                    // Проверяем, чтобы серийный номер оборудования был указан для каждой точки
                    if (points.Device != null)
                    {
                        measurepointNumber = points.Number;

                        string serial = points.Device.SerialNumber;

                        //Объявляем модель оборудования
                        EquipmentModel equipmentModel = await lersServer.Equipment.GetModelByIdAsync(points.Device.Model.Id);

                        xdoc.Element("settings").Add(_measurepoint = new XElement("measurePoint",  // Блок "точка учета" в XML файле       
                            new XElement("syncNumber", measurepointNumber), // Блок "номер точки учета" в XML файле
                            new XElement("title", points.FullTitle), // Блок "наименование точки учета" в XML файле
                            new XElement("address", points.Address), // Блок "Адрес точки учета" в XML файле
                            new XElement("systemType", points.SystemType.GetHashCode()), // Блок "Тип точки учета" в XML файле
                            _isDoublePipeHotWaterSystem = new XElement("isDoublePipeHotWaterSystem"), // Блок "определяем инженерную систему для ГВС" в XML файле 
                            new XElement("counterId", points.Id), // Блок "номер по порядку точки учета" в XML файле
                            new XElement("counterModel", points.Device.Model.Id), // Блок "ID оборудования точки учета" в XML файле
                            new XElement("counterSerialNumber", serial), // Блок "серийный номер оборудования точки учета" в XML файле
                            new XElement("networkAddress", points.Device.NetworkAddress), // Блок "сетевой адрес устройства точки учета" в XML файле
                            new XElement("password", points.Device.Password), // Блок "пароль для доступа к устройству точки учета" в XML файле
                            new XElement("protocolId", 0),  // Блок "протокол передачи данных устройства точки учета" в XML файле
                            new XElement("adapterModel", 0), // Блок "модель адаптера устройства точки учета" в XML файле
                            new XElement("adapterAddress", points.Device.PollSettings.AdapterAddress), // Блок "адрес адаптера устройства точки учета" в XML файле
                            _portSpeed = new XElement("portSpeed"), // Блок "скорость порта опроса оборудования" в XML файле
                            new XElement("flowControl", equipmentModel.DataInterface.SupportedFlowControls.GetHashCode()), // Блок "вид управления потоком при обмене данными через COM-порт устройства точки учета" в XML файле
                            heatLeadIn = new XElement("heatLeadIn", 1) // Блок "Номер теплового ввода точки учета" в XML, если пустое значение - Ставим по умолчанию "1"
                         ));

                        //Значение, определяющее, что эта точка учета имеет инженерную систему двухтрубного ГВС
                        if (points.IsDoublePipeHotWaterSystem)
                            _isDoublePipeHotWaterSystem.Value = "true";
                        else _isDoublePipeHotWaterSystem.Value = "false";

                        //В моем случае - скорость порта на ВКТ настроена на 19200 
                        if (points.Device.Model.Id == DeviceModel.Vkt9.GetHashCode())
                            _portSpeed.Value = "19200";
                        else _portSpeed.Value = "9600";

                        // Объявляем экземпляр оборудования
                        Equipment equipment = await lersServer.Equipment.GetByIdAsync(points.Device.Id, EquipmentInfo.Bindings);

                        if (points.SystemType != SystemType.Electricity) // Электичество не поддерживает связь точки учета с устройством
                        {
                            //Связь точки учета с устройством
                            try
                            {
                                DeviceChannel[] deviceChannelCollection = equipment.Bindings.Channels;
                                foreach (DeviceChannel deviceChannel in deviceChannelCollection)
                                {
                                    //Проверяем чтобы номер точки учета совпадал  
                                    int? channelNumber = deviceChannel.MeasurePoint.Number;
                                    if (measurepointNumber == channelNumber)
                                    {
                                        // Блок "Номер теплового ввода точки учета" в XML файле
                                        //_heatLeadIn = deviceChannel.HeatLeadIn.ToString();
                                        _heatLeadIn = deviceChannel.HeatLeadIn.ToString();
                                        heatLeadIn.Value = _heatLeadIn;

                                        //Записываем значения для подающей магистрали 
                                        if (deviceChannel.IsSupply)
                                        {
                                            _supplyChannel = deviceChannel.ChannelNumber.ToString();
                                            _returnChannel = "0";
                                            _measurepoint.Add(new XElement("supplyChannel", _supplyChannel), // Блок "подающая магистраль точки учета" в XML файле
                                                new XElement("returnChannel", _returnChannel)); // Блок "обратная магистраль точки учета" в XML файле
                                        }
                                        //Записываем значения для обратной магистрали 
                                        else
                                        {
                                            _returnChannel = deviceChannel.ChannelNumber.ToString();
                                            _measurepoint.SetElementValue("returnChannel", _returnChannel);
                                        }
                                    }
                                }
                            }
                            catch { }

                            // Блок "Ячейки"
                            try
                            {
                                DeviceCell[] cellsCollection = equipment.Bindings.Cells;
                                foreach (DeviceCell cells in cellsCollection)
                                {
                                    int? cellNumber = cells.MeasurePoint.Number;
                                    //Привязка ячейки устройства к параметру точки учета
                                    if (measurepointNumber == cellNumber)
                                    {
                                        // Блок "Описание ячейки данных устройства точки учета" в XML файле
                                        _measurepoint.Add(
                                            new XElement("cell", // Блок "Описание ячейки данных устройства точки учета" в XML файле
                                                new XElement("parameterId", cells.DataParameter.GetHashCode()),  // Блок "Параметр данных точки учета, данные по которому считываются из этой ячейки устройства" в XML файле
                                                new XElement("dataType", 0),
                                                new XElement("cellId", cells.Cell.Id), // Блок "ID ячейки данных точки учета" в XML файле
                                                new XElement("unit", cells.Unit.GetHashCode()), // Блок "Единица измерения данных в ячейке точки учета" в XML файле
                                                new XElement("pulseRatio", 1) // Блок "Вес импульса точки учета" в XML файле. В связи с выходом версии R22.05 - данный параметр не используется
                                                ));
                                    }
                                }
                            }
                            catch { }
                        }
                        else
                        {
                            heatLeadIn.Value = "0";
                            _measurepoint.Add(new XElement("supplyChannel", 0), // Блок "подающая магистраль точки учета" в XML файле
                                           new XElement("returnChannel", 0)); // Блок "обратная магистраль точки учета" в XML файле

                        }
                        // Блок "Задержка ответа устройства точки учета (в миллисекундах)" в XML файле
                        _measurepoint.Add(new XElement("responseDelay", points.Device.PollSettings.ResponseDelay));
                    }
                }
            }
            catch (RequestDisconnectException disconnect)
            {
                MessageBox.Show(disconnect.Message, "Соединение с сервером разорвано", MessageBoxButtons.OK, MessageBoxIcon.Error);
                tbLogin.Select();
                return;
            }
            #endregion

            // Сохраняем все данные в файл 
            xdoc.Save(pathToXmlFile);

            // Расячитываем время выполнения
            dateTimeEnd = DateTime.Now;
            TimeSpan timeSpan = dateTimeEnd - dateTimeStart;
            int minute = timeSpan.Minutes;
            int seconds = timeSpan.Seconds;
            int milliseconds = timeSpan.Milliseconds;

            //Останавливаем прогрессбар
            waitProgressBar.Style = ProgressBarStyle.Continuous;

            //Выводим сообщение об удачном завершении 
            MessageBox.Show(string.Format("Создание конфигурационного файла прошло успешно. \nВремя выполнения: {0} мин. {1} сек. {2} мс", minute, seconds, milliseconds), "Экспорт точек учета", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Восстанавливаем по умолчанию все элементы
            tbLogin.Text = "";
            tbPassword.Text = "";
            tbLogin.Enabled = true;
            tbPassword.Enabled = true;
            butExport.Enabled = false;
            butEnter.Enabled = true;
            butCancel.Enabled = false;
            tbLogin.Select();

            //Отключаемся от сервера через 2 секунды
            lersServer.Disconnect(2000);
        }
    }
}
