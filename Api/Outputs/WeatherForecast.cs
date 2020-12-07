using System;

namespace Api
{
    /// <summary>
    /// Weather Forecast class.
    /// </summary>
    public class WeatherForecast
    {
        /// <summary>
        /// When.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        ///  Temperature C.
        /// </summary>
        public int TemperatureC { get; set; }

        /// <summary>
        ///  Temperature F
        /// </summary>
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        /// <summary>
        ///  Summary of the forecast.
        /// </summary>
        public string Summary { get; set; }
    }
}
