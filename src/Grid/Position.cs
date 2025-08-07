using System;

namespace RPGGame.Grid
{
    /// <summary>
    /// Position on 2D grid (expandable to 3D with Z coordinate)
    /// </summary>
    public class Position
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; } = 0; // Future 3D support
        
        public Position(int x, int y, int z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        // Calculate distance between positions
        public double DistanceTo(Position other)
        {
            int dx = X - other.X;
            int dy = Y - other.Y;
            int dz = Z - other.Z;
            return Math.Sqrt(dx*dx + dy*dy + dz*dz);
        }
        
        public bool IsAdjacent(Position other)
        {
            return DistanceTo(other) <= 1.5; // Allows diagonal movement
        }
        
        public override string ToString()
        {
            return Z == 0 ? $"({X},{Y})" : $"({X},{Y},{Z})";
        }
        
        public override bool Equals(object obj)
        {
            if (obj is Position pos)
                return X == pos.X && Y == pos.Y && Z == pos.Z;
            return false;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }
    }
}
