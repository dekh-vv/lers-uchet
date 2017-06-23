using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lers;
using Lers.Core;
using System.Security;
using System.Xml;
using Lers.Poll;


namespace MeasurePointsExport
{
    public partial class MainForm : Form
    {
        Lers.LersServer lersServer;

        public MainForm()
        {
            InitializeComponent();
        }

        public void LersConnect()
        {
            lersServer = new LersServer();

            string login = tbLogin.Text;
            string secureString = tbPassword.Text;

            SecureString password = new SecureString();
            foreach (char c in secureString)
            {
                password.AppendChar(c);
            }
            
            Lers.Networking.BasicAuthenticationInfo authInfo = new Lers.Networking.BasicAuthenticationInfo(login, password);

            try
            {
                lersServer.Connect("localhost", 10000, authInfo);
            }
            catch (Exception ex)
            {
               MessageBox.Show("Ошибка подключения к серверу.\r\n" + ex.Message);
               return;
            }

        }

        private void butEnter_Click(object sender, EventArgs e)
        {
            LersConnect();
            if (lersServer.IsConnected)
            {
                butExport.Enabled = true;
            }
            else
            {
                tbLogin.Text = "";
                tbPassword.Text = "";
                butExport.Enabled = false;
            }
        }

        private void butExport_Click(object sender, EventArgs e)
        {
            
            //Директория сохраннения сгенерированного файла
            string pathToXmlFile = Environment.CurrentDirectory + "\\" + "UniversalReader-Settings.xml";

            #region Начинаем работу с XML
            //Создаем XML-файл
            XmlTextWriter textWriter = new XmlTextWriter(pathToXmlFile, Encoding.UTF8);
            
            //Создаём в файле заголовок XML-документа
            textWriter.WriteStartDocument();

            //Создём голову (head)
            textWriter.WriteStartElement("settings");

            //Закрываем голову
            textWriter.WriteEndElement();

            //Закрываем XmlTextWriter
            textWriter.Close();

            //Для занесения данных мы будем использовать класс XmlDocument
            XmlDocument document = new XmlDocument();
            
            //Загружаем файл
            document.Load(pathToXmlFile);
            #endregion 

            #region Создаем XML-записи
            //Номер COM порта
            XmlNode serialPortName = document.CreateElement("serialPortName");
            document.DocumentElement.AppendChild(serialPortName);
            serialPortName.InnerText = "";


            XmlNode listenPort = document.CreateElement("listenPort");
            document.DocumentElement.AppendChild(listenPort);
            listenPort.InnerText = "10000";
            
            //Директория хранения данных с опроса 
            XmlNode saveDirectory = document.CreateElement("saveDirectory");
            saveDirectory.InnerText = "C:\\Program Files\\LERS\\UniversalReader\\LERS";
            document.DocumentElement.AppendChild(saveDirectory);
           
            #region Эспорт точек в конфигурационный файл настроек Универсальнго пульта

            DateTime dateTimeStart = DateTime.Now;
     
            //Выводим все точки учета 
            MeasurePoint[] measurePointsCollection = lersServer.MeasurePoints.GetList(MeasurePointType.Regular, MeasurePointInfoFlags.Attributes | MeasurePointInfoFlags.Equipment);
            foreach (MeasurePoint points in measurePointsCollection)
            {
                // Проверяем, чтобы серийный номер оборудования был указан для каждой точки
                if (points.Device != null)
                {
                    // Объявляем эсземпляр оборудования 
                    Equipment equipment = lersServer.Equipment.GetById(points.Device.Id, EquipmentInfo.Bindings);
                    
                    var serial = points.Device.SerialNumber;

                    //Объявляем модель оборудования
                    EquipmentModel equipmentModel = lersServer.Equipment.GetModelById(points.Device.Model.Id);

                    // Блок "точка учета" в XML файле 
                    XmlNode measurePointXML = document.CreateElement("measurePoint");
                    document.DocumentElement.AppendChild(measurePointXML);

                    // Блок "номер точки учета" в XML файле
                    XmlNode syncNumber = document.CreateElement("syncNumber");
                    syncNumber.InnerText = points.Number.ToString();
                    measurePointXML.AppendChild(syncNumber);

                    // Блок "наименование точки учета" в XML файле
                    XmlNode title = document.CreateElement("title");
                    title.InnerText = points.FullTitle;
                    measurePointXML.AppendChild(title);

                    // Блок "Адрес точки учета" в XML файле
                    XmlNode address = document.CreateElement("address");
                    address.InnerText = points.Address;
                    measurePointXML.AppendChild(address);

                    // Блок "Тип точки учета" в XML файле
                    XmlNode systemType = document.CreateElement("systemType");
                    systemType.InnerText = points.SystemType.GetHashCode().ToString();
                    measurePointXML.AppendChild(systemType);

                    // Блок "определяем инженерную систему для ГВС" в XML файле
                    if (points.SystemType == SystemType.HotWater)
                    {
                        // Значение, определяющее, что эта точка учета имеет инженерную систему двухтрубного ГВС
                        XmlNode isDoublePipeHotWaterSystemXML = document.CreateElement("isDoublePipeHotWaterSystem");
                        if (points.IsDoublePipeHotWaterSystem)
                        {
                            isDoublePipeHotWaterSystemXML.InnerText = "true";
                        }
                        else
                        {
                            isDoublePipeHotWaterSystemXML.InnerText = "false";
                        }
                        measurePointXML.AppendChild(isDoublePipeHotWaterSystemXML);
                    }

                    // Блок "номер по порядку точки учета" в XML файле
                    XmlNode counterId = document.CreateElement("counterId");
                    counterId.InnerText = points.Id.ToString();
                    measurePointXML.AppendChild(counterId);

                    // Блок "ID оборудования точки учета" в XML файле
                    XmlNode counterModel = document.CreateElement("counterModel");
                    counterModel.InnerText = points.Device.Model.Id.ToString();
                    measurePointXML.AppendChild(counterModel);

                    // Блок "серийный номер оборудования точки учета" в XML файле
                    XmlNode counterSerialNumber = document.CreateElement("counterSerialNumber");
                    counterSerialNumber.InnerText = serial;
                    measurePointXML.AppendChild(counterSerialNumber);

                    // Блок "сетевой адрес устройства точки учета" в XML файле
                    XmlNode networkAddress = document.CreateElement("networkAddress");
                    networkAddress.InnerText = points.Device.NetworkAddress;
                    measurePointXML.AppendChild(networkAddress);

                    // Блок "пароль для доступа к устройству точки учета" в XML файле
                    XmlNode passwordXML = document.CreateElement("password");
                    passwordXML.InnerText = points.Device.Password;
                    measurePointXML.AppendChild(passwordXML);

                    // Блок "протокол передачи данных устройства точки учета" в XML файле
                    XmlNode protocolId = document.CreateElement("protocolId");
                    protocolId.InnerText = "0";
                    measurePointXML.AppendChild(protocolId);

                    // Блок "модель адаптера устройства точки учета" в XML файле
                    XmlNode adapterModel = document.CreateElement("adapterModel");
                    adapterModel.InnerText = "0";
                    measurePointXML.AppendChild(adapterModel);

                    // Блок "адрес адаптера устройства точки учета" в XML файле
                    XmlNode adapterAddress = document.CreateElement("adapterAddress");
                    adapterAddress.InnerText = "0";
                    measurePointXML.AppendChild(adapterAddress);

                    // Блок "скорость порта опроса оборудования" в XML файле
                    XmlNode portSpeed = document.CreateElement("portSpeed");
                    //В моем случае - скорость порта на ВКТ настроена на 19200 
                    if (points.Device.Model.Id == 179) // id ВКТ = 179 
                    { portSpeed.InnerText = "19200"; }
                    //Для остального оборудвания стандартная скорость 
                    else { portSpeed.InnerText = "9600"; }
                    measurePointXML.AppendChild(portSpeed);

                    // Блок "вид управления потоком при обмене данными через COM-порт устройства точки учета" в XML файле
                    XmlNode flowControl = document.CreateElement("flowControl");
                    flowControl.InnerText = equipmentModel.DataInterface.SupportedFlowControls.GetHashCode().ToString();
                    measurePointXML.AppendChild(flowControl);

                    // Блок "Номер теплового ввода точки учета" в XML файле
                    XmlNode heatLeadIn = document.CreateElement("heatLeadIn");

                    // Блок "подающая магистраль точки учета" в XML файле
                    XmlNode supplyChannel = document.CreateElement("supplyChannel");
                    // Блок "обратная магистраль точки учета" в XML файле
                    XmlNode returnChannel = document.CreateElement("returnChannel");

                    //Связь точки учета с устройством
                    foreach (DeviceChannel deviceChannel in equipment.Bindings.Channels)
                    {
                        //Проверяем чтобы номер точки учета совпадал  
                        if (points.Number == deviceChannel.MeasurePoint.Number)
                        {
                            // Блок "Номер теплового ввода точки учета" в XML файле
                            heatLeadIn.InnerText = deviceChannel.HeatLeadIn.ToString();
                            measurePointXML.AppendChild(heatLeadIn);

                            //Записываем значения для подающей магистрали 
                            if (deviceChannel.IsSupply)
                            {
                                supplyChannel.InnerText = deviceChannel.ChannelNumber.ToString();
                                returnChannel.InnerText = "0";
                            }
                            //Записываем значения для обратной магистрали 
                            else
                            {
                                returnChannel.InnerText = deviceChannel.ChannelNumber.ToString();
                            }
                            measurePointXML.AppendChild(supplyChannel);
                            measurePointXML.AppendChild(returnChannel);
                        }
                    }

                    try
                    {
                        foreach (DeviceCell cells in equipment.Bindings.Cells)
                        {
                            //Привязка ячейки устройства к параметру точки учета
                            if (points.Number == cells.MeasurePoint.Number)
                            {
                                // Блок "Описание ячейки данных устройства точки учета" в XML файле
                                XmlNode cell = document.CreateElement("cell");
                                measurePointXML.AppendChild(cell);

                                // Блок "Параметр данных точки учета, данные по которому считываются из этой ячейки устройства" в XML файле
                                XmlNode parameterId = document.CreateElement("parameterId");
                                parameterId.InnerText = cells.DataParameter.GetHashCode().ToString();
                                cell.AppendChild(parameterId);


                                XmlNode dataType = document.CreateElement("dataType");
                                dataType.InnerText = "0";
                                cell.AppendChild(dataType);

                                // Блок "ID ячейки данных точки учета" в XML файле
                                XmlNode cellId = document.CreateElement("cellId");
                                cellId.InnerText = cells.Cell.Id.ToString();
                                cell.AppendChild(cellId);

                                // Блок "Единица измерения данных в ячейке точки учета" в XML файле
                                XmlNode unit = document.CreateElement("unit");
                                unit.InnerText = cells.Unit.GetHashCode().ToString();
                                cell.AppendChild(unit);

                                // Блок "Вес импульса точки учета" в XML файле. В связи с выходом версии R22.05 - данный параметр не используется
                                // Для ранних версий этот параметр обязателен 
                                //XmlNode pulseRatio = document.CreateElement("pulseRatio");
                                //pulseRatio.InnerText = "1";
                                //cell.AppendChild(pulseRatio);
                            }
                        }
                    }
                    catch { }

                    // Блок "Задержка ответа устройства точки учета (в миллисекундах)" в XML файле
                    XmlNode responseDelay = document.CreateElement("responseDelay");
                    responseDelay.InnerText = points.Device.PollSettings.ResponseDelay.ToString();
                    measurePointXML.AppendChild(responseDelay);
                }
            }
            DateTime dateTimeEnd = DateTime.Now;
            TimeSpan timeSpan = dateTimeEnd - dateTimeStart;
            double iteration_time_simple = (double)timeSpan.TotalMilliseconds / 1000;
            #endregion 

            #endregion

            //Сохраняем все данные в файл
            document.Save(pathToXmlFile);

            //Выводим сообщение об удачном завершении 
            MessageBox.Show("Создание конфигурационного файла прошло успешно. \nВремя выполнения: " + iteration_time_simple + " сек.", "Экспорт точек учета", MessageBoxButtons.OK, MessageBoxIcon.Information);

            tbLogin.Text = "";
            tbPassword.Text = "";
            butExport.Enabled = false;

            //Отключаемся от сервера через 2 секунды
            lersServer.Disconnect(2000);

            
        }
    }
}
