using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GPS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Port szeregowy w celu nawiązania połączenia
        /// </summary>
        static SerialPort _serialPort;

        /// <summary>
        /// Adres strony www do wyświetlenia mapy
        /// </summary>
        string googleMapsUrl = "https://www.google.com/maps/search/?api=1&query=";

        /// <summary>
        /// Opóźnienie
        /// </summary>
        int delay = 1000;

        string outputData;
        string latitude;
        string longitude;

        /// <summary>
        /// Konstruktor
        /// </summary>
        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
            InitializeComponent();
            _serialPort = new SerialPort();
        }

        /// <summary>
        /// Odczytanie danych z modułu GPS
        /// </summary>
        private void GetData()
        {
            outputData = _serialPort.ReadExisting();
            var splitedData = outputData.Split('$');
    
            //Pętla po odebranych danych
            foreach(var line in splitedData)
            {
                try
                {
                    if(line.Contains("GPGGA"))
                    {
                        string fetchedLatitude = "";
                        string fetchedLongitude = "";

                        var info = line.Split(',');

                        string latitude = info[2];
                        string longitude = info[4];
                        double longdec = double.Parse(info[4], CultureInfo.InvariantCulture) / 100.0;
                        double latdec = double.Parse(info[2], CultureInfo.InvariantCulture) / 100.0;
                        if (info[3] == "S")
                        {
                            fetchedLatitude = "-";
                        }
                        if (info[3] == "W")
                        {
                            fetchedLongitude = "-";
                        }
                        var latSplit = Convert.ToString(latdec).Split('.');
                        var longSplit = Convert.ToString(longdec).Split('.');

                        longdec = Convert.ToDouble("0." + longSplit[1], CultureInfo.InvariantCulture) * 10 / 6;
                        latdec = Convert.ToDouble("0." + latSplit[1], CultureInfo.InvariantCulture) * 10 / 6;

                        LatitudeTextBox.Text = fetchedLatitude + (Convert.ToDouble(latSplit[0]) + latdec).ToString("F4");
                        LongitudeTextBox.Text = fetchedLongitude + (Convert.ToDouble(longSplit[0]) + longdec).ToString("F4");

                        latitude = fetchedLatitude + (Convert.ToDouble(latSplit[0]) + latdec).ToString("F4");
                        longitude = fetchedLongitude + (Convert.ToDouble(longSplit[0]) + longdec).ToString("F4");

                        HeightAboveSeaLevelTextBox.Text = info[9] + " m";
                        NumberOfSatelitesTextBox.Text = info[7];
                    }
                }
                catch (Exception)
                {}
            }
        }

        /// <summary>
        /// Połączenie z urządzeniem Bluetooth
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            _serialPort.PortName = PortNameTextBox.Text;
            _serialPort.BaudRate = 9600;
            _serialPort.Open();

            //Uruchomienie wątku
            Task.Run(() =>
            {
                while(true)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => { GetData(); }));
                    Thread.Sleep(delay);
                }
            });
        }

        /// <summary>
        /// Wskazanie na mapie aktualnej lokalizacji
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowOnMapButton_Click(object sender, RoutedEventArgs e)
        {
            webBrowser.Navigated += new NavigatedEventHandler(WebBrowser_Navigated);
            webBrowser.Navigate(googleMapsUrl + latitude + "," + longitude);
        }

        /// <summary>
        /// Delegat czyszczenia komunikatów
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void WebBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            HideJsScriptErrors((WebBrowser)sender);
        }

        /// <summary>
        /// Ukrycie komunikatów JS
        /// </summary>
        /// <param name="wb"></param>
        public void HideJsScriptErrors(WebBrowser wb)
        {
            FieldInfo fld = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fld == null)
                return;
            object obj = fld.GetValue(wb);
            if (obj == null)
                return;

            obj.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, obj, new object[] { true });
        }
    }
}
