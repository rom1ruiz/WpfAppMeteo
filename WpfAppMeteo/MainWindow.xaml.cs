using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WpfAppMeteo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public string startPath = Directory.GetCurrentDirectory();
        public string ApiKey = "4119fed4e4efeaee2d5428f9bc8e4a69";
        public string geoCodeApi = "http://api.openweathermap.org/geo/1.0/direct?q=";
        public string geoSelect = "&limit=5&appid=";
        public string weatherApi = "https://api.openweathermap.org/data/2.5/weather?";
        public string forecastApi = "https://api.openweathermap.org/data/2.5/forecast?";
        public string city = "Strasbourg";
        public string previous = "";
        public string jsonStringGeo = "";
        public DoubleAnimation animation = new DoubleAnimation(1, TimeSpan.FromMilliseconds(1000));



        public MainWindow()
        {
            InitializeComponent();
            lbCityName.Content = city;
            SetFadeInForecast();
            SetDay();
            getGeoApi();

        }
        public void getGeoApi()
        {
            string URL = geoCodeApi + city + "&lang=fr&units=metric" + geoSelect + ApiKey;
            HttpClient client = new HttpClient();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.Timeout = 5000;
            request.Credentials = CredentialCache.DefaultCredentials;
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();


                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (WebClient wc = new WebClient())
                    {
                        //Téléchargement du JSON depuis l'API Geo

                        string geoQuery = geoCodeApi + city + "&lang=fr&units=metric" + geoSelect + ApiKey;



                        string jsonGeo = wc.DownloadString(geoQuery);
                        if (jsonGeo != "[]")
                        {
                            jsonGeo = "{select:" + jsonGeo + "}";
                            //Encodage UTF-8 de la réponse
                            byte[] bytesGeo = Encoding.Default.GetBytes(jsonGeo);
                            jsonGeo = Encoding.UTF8.GetString(bytesGeo);

                            ////Formattage de la requête
                            dynamic geoJson = JsonConvert.DeserializeObject(jsonGeo);

                            //If the city name change entirely
                            string[] splitCity = city.Split(",");

                            string[] splitPrev = previous.Split(",");
                            if (splitCity[0] != splitPrev[0] && previous != null)
                            {
                                ctyText.Items.Clear();
                                //MessageBox.Show($"Clear [ city = {splitCity[0]} ] [ previous = {splitPrev[0]} ]");
                            }
                            //Just in case the next city isn't valid
                            previous = city;


                            string lat = geoJson.select[0].lat;
                            string lon = geoJson.select[0].lon;
                            string ctry = geoJson.select[0].country;
                            if (geoJson.select[0].state != null)
                            {
                                ctyText.Text = geoJson.select[0].name + "," + geoJson.select[0].state + "," + geoJson.select[0].country;
                            }
                            else
                            {
                                ctyText.Text = geoJson.select[0].name + "," + geoJson.select[0].country;
                            }
                            //if (ctyText.Items.Count < 5)
                            //{
                            for (int i = 0; i < geoJson.select.Count; i++)
                            {
                                ctyText.Items.Add(geoJson.select[i].name + "," + geoJson.select[i].state + "," + geoJson.select[i].country);
                            }
                            //}
                            ctry = ctry.ToLower();
                            getweatherApi(lat, lon);

                        }
                        else
                        {
                            MessageBox.Show("La ville insérée est introuvable ou n'existe pas");
                        }
                    }


                }


                else
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        MessageBox.Show("Problème de connexion veuillez vérifier votre accès à Internet");
                        Console.WriteLine("---> HTTP Response code =  404");
                    }
                    else if (response.StatusCode == HttpStatusCode.InternalServerError || response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        MessageBox.Show("Accès au serveur impossible ou service indisponilbe");
                        Console.WriteLine("---> HTTP Response code = 500 or 503");
                    }
                }

            }
            catch (System.Net.WebException e)
            {
                if (MessageBox.Show("Problème de connexion veuillez vérifier votre accès à Internet", "Echec de connexion", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
                {
                    Console.WriteLine(e.Message);
                    this.Close();
                }


            }


        }


        public void getweatherApi(string lat, string lon)
        {
            using (WebClient wc = new WebClient())
            {
                //Téléchargement du JSON depuis l'API Weather
                string weatherQuery = weatherApi + "lat=" + lat + "&lon=" + lon + "&lang=fr&units=metric&appid=" + ApiKey;
                string jsonWeather = wc.DownloadString(weatherQuery);
                //Encodage UTF-8 de la réponse
                byte[] bytesWea = Encoding.Default.GetBytes(jsonWeather);
                jsonWeather = Encoding.UTF8.GetString(bytesWea);
                dynamic weatherJson = JsonConvert.DeserializeObject(jsonWeather);
                int tempNow = (int)weatherJson.main.temp;
                lbTemp.Content = tempNow.ToString() + "°C";
                lbVent.Content = "Vent : " + weatherJson.wind.speed + " km/h";
                lbWet.Content = "Humidité : " + weatherJson.main.humidity + " %";
                lbWeatherSt.Content = weatherJson.weather[0].description;
                string weatherMain = weatherJson.weather[0].main;
                int timeZone = weatherJson.timezone;

                string utc = DateTime.UtcNow.AddSeconds(timeZone).ToString("HH:mm:ss");

                lbActual.Content = "Heure Actuelle :" + utc;
                DetermineDay(timeZone);

                string defHeader = defineHeader(weatherMain);
                headerImg.Source = new BitmapImage(new Uri(startPath + "/weather_status/" + defHeader));
                string[] splitCit = city.Split(",");
                lbCityName.Content = splitCit[0];
                //Téléchargement du JSON depuis l'API Weather pour prévision 4 jours
                string forecastQuery = forecastApi + "lat=" + lat + "&lon=" + lon + "&lang=fr&units=metric&appid=" + ApiKey;
                string jsonForecast = wc.DownloadString(forecastQuery);
                //Encode UTF-8 de la réponse
                byte[] bytesDail = Encoding.Default.GetBytes(jsonForecast);
                jsonForecast = Encoding.UTF8.GetString(bytesDail);

                //Traitement du JSON 
                dynamic FrcJson = JsonConvert.DeserializeObject(jsonForecast);

                //Mise en page des températures
                string tmpj1 = ((int)FrcJson.list[7].main.temp).ToString() + "°C";
                string tmpj2 = ((int)FrcJson.list[15].main.temp).ToString() + "°C";
                string tmpj3 = ((int)FrcJson.list[23].main.temp).ToString() + "°C";
                string tmpj4 = ((int)FrcJson.list[31].main.temp).ToString() + "°C";

                //Actualisation des températures
                TempJ1.Content = tmpj1;
                TempJ2.Content = tmpj2;
                TempJ3.Content = tmpj3;
                TempJ4.Content = tmpj4;

                //Récupération du statut météo
                string mainJ1 = FrcJson.list[7].weather[0].main;
                string mainJ2 = FrcJson.list[15].weather[0].main;
                string mainJ3 = FrcJson.list[23].weather[0].main;
                string mainJ4 = FrcJson.list[31].weather[0].main;
                string defJ1 = defineWeather(mainJ1);
                string defJ2 = defineWeather(mainJ2);
                string defJ3 = defineWeather(mainJ3);
                string defJ4 = defineWeather(mainJ4);
                weatherJ1.Source = new BitmapImage(new Uri(startPath + "/weather_status/" + defJ1));
                weatherJ2.Source = new BitmapImage(new Uri(startPath + "/weather_status/" + defJ2));
                weatherJ3.Source = new BitmapImage(new Uri(startPath + "/weather_status/" + defJ3));
                weatherJ4.Source = new BitmapImage(new Uri(startPath + "/weather_status/" + defJ4));
            }
        }

        public string defineHeader(string s)
        {
            if (s.Contains("Clouds"))
            {
                return "cloudy.jpg";
            }
            else if (s.Contains("Clear"))
            {
                return "sunny.jpg";
            }
            else if (s.Contains("Rain"))
            {
                return "rainy-day.jpg";
            }
            else if (s.Contains("Thunderstorm"))
            {

                return "cloud-lightning.jpg";
            }
            else if (s.Contains("Fog"))
            {
                return "foggy.jpg";
            }
            else if (s.Contains("Mist") || s.Contains("Drizzle"))
            {
                return "mist.jpg";
            }
            else if (s.Contains("Snow"))
            {
                return "snowy.jpg";
            }
            else if (s.Contains("Dust") || s.Contains("Sand"))
            {
                return "dust.jpg";
            }
            else
            {
                return "notfound.jpg";
            }
        }

        public string defineWeather(string s)
        {
            if (s.Contains("Clouds"))
            {
                return "clouds.png";
            }
            else if (s.Contains("Clear"))
            {
                return "sun.png";
            }
            else if (s.Contains("Rain"))
            {
                return "heavy-rain.png";
            }
            else if (s.Contains("Storm"))
            {
                return "cloud-lightning";
            }
            else if (s.Contains("Snow"))
            {
                return "snow.png";
            }
            else
            {
                return "clouds.png";
            }
        }
        void EnterClicked(object sender, KeyEventArgs e)
        {
            if (ctyText.Text != null && ctyText.Text != "" && e.Key == Key.Return)
            {
                city = ctyText.Text;
                SetFadeIn();
                SetFadeInForecast();
                getGeoApi();

                e.Handled = true;
            }
        }

        public void SetDay()
        {
            lbDate.Content = CapitalizeStr(DateTime.Now.ToString("dddd dd MMMM yyyy"));
            Jour1.Content = CapitalizeStr(DateTime.Now.AddDays(1).ToString("ddd"));
            Jour2.Content = CapitalizeStr(DateTime.Now.AddDays(2).ToString("ddd"));
            Jour3.Content = CapitalizeStr(DateTime.Now.AddDays(3).ToString("ddd"));
            Jour4.Content = CapitalizeStr(DateTime.Now.AddDays(4).ToString("ddd"));
        }


        public void DetermineDay(double d)
        {
            DateTime now = DateTime.Now;

            lbDate.Content = CapitalizeStr(now.AddSeconds(d).ToString("dddd dd MMMM yyyy"));
            Jour1.Content = CapitalizeStr(now.AddDays(1).AddSeconds(d).ToString("ddd"));
            Jour2.Content = CapitalizeStr(now.AddDays(2).AddSeconds(d).ToString("ddd"));
            Jour3.Content = CapitalizeStr(now.AddDays(3).AddSeconds(d).ToString("ddd"));
            Jour4.Content = CapitalizeStr(now.AddDays(4).AddSeconds(d).ToString("ddd"));
        }

        public string CapitalizeStr(string str)
        {
            str = char.ToUpper(str[0]) + str.Substring(1);
            return str;
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (ctyText != null)
            {
                city = ctyText.Text;
                SetFadeIn();
                SetFadeInForecast();
                getGeoApi();

                e.Handled = true;
            }


        }


        public void SetFadeIn()
        {

            lbCityName.Opacity = 0;
            lbWeatherSt.Opacity = 0;
            headerImg.Opacity = 0;
            lbActual.Opacity = 0;
            lbDate.Opacity = 0;
            lbTemp.Opacity = 0;
            lbVent.Opacity = 0;
            lbWet.Opacity = 0;

            //RESET DE L'ANIMATION

            //Nom de la ville
            lbCityName.BeginAnimation(Label.OpacityProperty, null);

            //Statut de la météo actuel
            lbWeatherSt.BeginAnimation(Label.OpacityProperty, null);

            //heure actuelle en UTC
            lbActual.BeginAnimation(Label.OpacityProperty, null);

            //Image représentative de la météo actuel
            headerImg.BeginAnimation(Image.OpacityProperty, null);

            //Date du jour
            lbDate.BeginAnimation(Label.OpacityProperty, null);

            //Témpérature actuel
            lbTemp.BeginAnimation(Label.OpacityProperty, null);

            //Force du vent
            lbVent.BeginAnimation(Label.OpacityProperty, null);

            //Humidité
            lbWet.BeginAnimation(Label.OpacityProperty, null);



            //Nom de la ville
            lbCityName.BeginAnimation(Label.OpacityProperty, animation);

            //Statut de la météo actuel
            lbWeatherSt.BeginAnimation(Label.OpacityProperty, animation);

            //Heure actuelle
            lbActual.BeginAnimation(Label.OpacityProperty, animation);

            //Image représentative de la météo actuel
            headerImg.BeginAnimation(Image.OpacityProperty, animation);

            //Date du jour
            lbDate.BeginAnimation(Label.OpacityProperty, animation);

            //Témpérature actuel
            lbTemp.BeginAnimation(Label.OpacityProperty, animation);

            //Force du vent
            lbVent.BeginAnimation(Label.OpacityProperty, animation);

            //Humidité
            lbWet.BeginAnimation(Label.OpacityProperty, animation);
        }

        public async void SetFadeInForecast()
        {

            Jour1.Opacity = 0;
            Jour2.Opacity = 0;
            Jour3.Opacity = 0;
            Jour4.Opacity = 0;
            TempJ1.Opacity = 0;
            TempJ2.Opacity = 0;
            TempJ3.Opacity = 0;
            TempJ4.Opacity = 0;
            weatherJ1.Opacity = 0;
            weatherJ2.Opacity = 0;
            weatherJ3.Opacity = 0;
            weatherJ4.Opacity = 0;

            //RESET DE L'ANIMATION

            // JOUR 1
            Jour1.BeginAnimation(Label.OpacityProperty, null);
            TempJ1.BeginAnimation(Label.OpacityProperty, null);
            weatherJ1.BeginAnimation(Image.OpacityProperty, null);

            // JOUR 2
            TempJ2.BeginAnimation(Label.OpacityProperty, null);
            Jour2.BeginAnimation(Label.OpacityProperty, null);
            weatherJ2.BeginAnimation(Image.OpacityProperty, null);

            // JOUR 3
            TempJ3.BeginAnimation(Label.OpacityProperty, null);
            Jour3.BeginAnimation(Label.OpacityProperty, null);
            weatherJ3.BeginAnimation(Image.OpacityProperty, null);

            // JOUR 4
            TempJ4.BeginAnimation(Label.OpacityProperty, null);
            Jour4.BeginAnimation(Label.OpacityProperty, null);
            weatherJ4.BeginAnimation(Image.OpacityProperty, null);


            //DEBUT DE L'ANIMATION

            await Task.Delay(1500);
            // JOUR 1
            Jour1.BeginAnimation(Label.OpacityProperty, animation);
            TempJ1.BeginAnimation(Label.OpacityProperty, animation);
            weatherJ1.BeginAnimation(Image.OpacityProperty, animation);
            await Task.Delay(500);
            // JOUR 2
            TempJ2.BeginAnimation(Label.OpacityProperty, animation);
            Jour2.BeginAnimation(Label.OpacityProperty, animation);
            weatherJ2.BeginAnimation(Image.OpacityProperty, animation);
            await Task.Delay(500);
            // JOUR 3
            TempJ3.BeginAnimation(Label.OpacityProperty, animation);
            Jour3.BeginAnimation(Label.OpacityProperty, animation);
            weatherJ3.BeginAnimation(Image.OpacityProperty, animation);
            await Task.Delay(500);
            // JOUR 4
            TempJ4.BeginAnimation(Label.OpacityProperty, animation);
            Jour4.BeginAnimation(Label.OpacityProperty, animation);
            weatherJ4.BeginAnimation(Image.OpacityProperty, animation);
            await Task.Delay(500);
        }


    }
}
