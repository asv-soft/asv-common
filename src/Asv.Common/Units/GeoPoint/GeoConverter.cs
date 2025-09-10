using System;

namespace Asv.Common
{
    public static class GeoConverter
    {
        private static readonly double Pi = 3.14159265358979; // Число Пи
        private static readonly double Ro = 206264.8062; // Число угловых секунд в радиане

        // Эллипсоид Красовского
        private static readonly double AP = 6378136; // Большая полуось
        private static readonly double AlP = 1 / 298.257839303; // Сжатие
        private static readonly double E2P = (2 * AlP) - Math.Pow(AlP, 2); // Квадрат эксцентриситета

        // Элипсоид WGS84 (GRS80, эти два эллипсоида сходны по большинству параметров)
        private static readonly double AW = 6378137; // Большая полуось
        private static readonly double AlW = 1 / 298.257223563; // Сжатие
        private static readonly double E2W = (2 * AlW) - Math.Pow(AlW, 2); // Квадрат эксцентриситета

        // Вспомогательные значения для преобразования эллипсоидов
        private static readonly double A = (AP + AW) / 2;
        private static readonly double E2 = (E2P + E2W) / 2;
        private static readonly double Da = AW - AP;
        private static readonly double De2 = E2W - E2P;

        // Линейные элементы трансформирования, в метрах
        private static readonly double Dx4284 = 28;
        private static readonly double Dy4284 = -130;
        private static readonly double Dz4284 = -95;

        private static readonly double Dx4290 = 23.92;
        private static readonly double Dy4290 = -141.27;
        private static readonly double Dz4290 = -80.9;

        private static readonly double Dx9084 = -1.08;
        private static readonly double Dy9084 = -0.27;
        private static readonly double Dz9084 = -0.9;

        // Угловые элементы трансформирования, в секундах
        private static readonly double Wx = 0;
        private static readonly double Wy = 0;
        private static readonly double Wz = 0;

        // Дифференциальное различие масштабов
        private static readonly double Ms = 0;

        public static GeoPoint PZ90_WGS84(this GeoPoint point)
        {
            var lat = PZ90_WGS84_Lat(point.Latitude, point.Longitude, point.Altitude);
            var lon = PZ90_WGS84_Long(point.Latitude, point.Longitude, point.Altitude);
            var alt = Wgs84Alt(
                point.Latitude,
                point.Longitude,
                point.Altitude,
                Dx9084,
                Dy9084,
                Dz9084
            );
            return new GeoPoint(lat, lon, alt);
        }

        public static GeoPoint WGS84_PZ90(this GeoPoint point)
        {
            var lat = WGS84_PZ90_Lat(point.Latitude, point.Longitude, point.Altitude);
            var lon = WGS84_PZ90_Long(point.Latitude, point.Longitude, point.Altitude);
            var alt = Wgs84Alt(
                point.Latitude,
                point.Longitude,
                point.Altitude,
                Dx9084,
                Dy9084,
                Dz9084
            );
            return new GeoPoint(lat, lon, alt);
        }

        private static double DB(double bd, double ld, double h, double dx, double dy, double dz)
        {
            double b,
                l,
                m,
                n;
            b = bd * Pi / 180;
            l = ld * Pi / 180;
            m = A * (1 - E2) / Math.Pow(1 - (E2 * Math.Pow(Math.Sin(b), 2)), 1.5);
            n = A * Math.Pow(1 - (E2 * Math.Pow(Math.Sin(b), 2)), -0.5);

            return (
                    Ro
                    / (m + h)
                    * (
                        (n / A * E2 * Math.Sin(b) * Math.Cos(b) * Da)
                        + (
                            ((Math.Pow(n, 2) / Math.Pow(A, 2)) + 1)
                            * n
                            * Math.Sin(b)
                            * Math.Cos(b)
                            * De2
                            / 2
                        )
                        - (((dx * Math.Cos(l)) + (dy * Math.Sin(l))) * Math.Sin(b))
                        + (dz * Math.Cos(b))
                    )
                )
                - (Wx * Math.Sin(l) * (1 + (E2 * Math.Cos(2 * b))))
                + (Wy * Math.Cos(l) * (1 + (E2 * Math.Cos(2 * b))))
                - (Ro * Ms * E2 * Math.Sin(b) * Math.Cos(b));
        }

        private static double DL(double bd, double ld, double h, double dx, double dy, double dz)
        {
            double b,
                l,
                n;
            b = bd * Pi / 180;
            l = ld * Pi / 180;
            n = A * Math.Pow(1 - (E2 * Math.Pow(Math.Sin(b), 2)), -0.5);
            return (Ro / ((n + h) * Math.Cos(b)) * ((-dx * Math.Sin(l)) + (dy * Math.Cos(l))))
                + (Math.Tan(b) * (1 - E2) * ((Wx * Math.Cos(l)) + (Wy * Math.Sin(l))))
                - Wz;
        }

        private static double Wgs84Alt(
            double bd,
            double ld,
            double h,
            double dx,
            double dy,
            double dz
        )
        {
            double b,
                l,
                n,
                dH;
            b = bd * Pi / 180;
            l = ld * Pi / 180;
            n = A * Math.Pow(1 - (E2 * Math.Pow(Math.Sin(b), 2)), -0.5);
            dH =
                (-A / n * Da)
                + (n * Math.Pow(Math.Sin(b), 2) * De2 / 2)
                + (((dx * Math.Cos(l)) + (dy * Math.Sin(l))) * Math.Cos(b))
                + (dz * Math.Sin(b))
                - (
                    n
                    * E2
                    * Math.Sin(b)
                    * Math.Cos(b)
                    * ((Wx / Ro * Math.Sin(l)) - (Wy / Ro * Math.Cos(l)))
                )
                + (((Math.Pow(A, 2) / n) + h) * Ms);
            return h + dH;
        }

        private static double PZ90_WGS84_Lat(double bd, double ld, double h)
        {
            return bd + (DB(bd, ld, h, Dx9084, Dy9084, Dz9084) / 3600);
        }

        private static double PZ90_WGS84_Long(double bd, double ld, double h)
        {
            return ld + (DL(bd, ld, h, Dx9084, Dy9084, Dz9084) / 3600);
        }

        private static double WGS84_PZ90_Lat(double bd, double ld, double h)
        {
            return bd - (DB(bd, ld, h, Dx9084, Dy9084, Dz9084) / 3600);
        }

        private static double WGS84_PZ90_Long(double bd, double ld, double h)
        {
            return ld - (DL(bd, ld, h, Dx9084, Dy9084, Dz9084) / 3600);
        }

        //
        private static double CK42_PZ90_Lat(double bd, double ld, double h)
        {
            return bd + (DB(bd, ld, h, Dx4290, Dy4290, Dz4290) / 3600);
        }

        private static double CK42_PZ90_Long(double bd, double ld, double h)
        {
            return ld + (DL(bd, ld, h, Dx4290, Dy4290, Dz4290) / 3600);
        }

        private static double PZ90_CK42_Lat(double bd, double ld, double h)
        {
            return bd - (DB(bd, ld, h, Dx4290, Dy4290, Dz4290) / 3600);
        }

        private static double PZ90_CK42_Long(double bd, double ld, double h)
        {
            return ld - (DL(bd, ld, h, Dx4290, Dy4290, Dz4290) / 3600);
        }

        //
        private static double CK42_WGS84_Lat(double bd, double ld, double h)
        {
            return bd + (DB(bd, ld, h, Dx4284, Dy4284, Dz4284) / 3600);
        }

        private static double CK42_WGS84_Long(double bd, double ld, double h)
        {
            return ld + (DL(bd, ld, h, Dx4284, Dy4284, Dz4284) / 3600);
        }

        private static double WGS84_CK42_Lat(double bd, double ld, double h)
        {
            return bd - (DB(bd, ld, h, Dx4284, Dy4284, Dz4284) / 3600);
        }

        public static double WGS84_CK42_Long(double bd, double ld, double h)
        {
            return ld - (DL(bd, ld, h, Dx4284, Dy4284, Dz4284) / 3600);
        }
    }
}
