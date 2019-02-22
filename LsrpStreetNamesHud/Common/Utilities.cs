using System;
using System.Drawing;

namespace LsrpStreetNamesHud.Common
{
    public class Utilities
    {
        /// <summary>
        /// Converts GTA:SA coordinates (which go from -3072 to +3072) to map coordinates (i.e. 0 to 6144)
        /// </summary>
        public static Point GameCoordinatesToMapCoordinates(Point gameCoordinates)
        {
            var mapX = gameCoordinates.X > 0 ? (gameCoordinates.X + 3072) : 3072 - Math.Abs(gameCoordinates.X);
            var mapY = gameCoordinates.Y < 0 ? (Math.Abs(gameCoordinates.Y) + 3072) : 3072 - gameCoordinates.Y;
            
            return new Point(mapX, mapY);
        }
    }
}
