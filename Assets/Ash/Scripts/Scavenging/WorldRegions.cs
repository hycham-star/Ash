using System;
using System.Collections.Generic;

namespace Ash
{
    public enum WorldBiome { DryGrassland, Badlands, AshWastes }
    public static class WorldRegions
    {
        public const int HalfSize=200,TileCount=401,LegacyCampCount=9,OriginalCampCount=256,CampCount=512;
        public static int Tile(float coordinate)
        {
            if(float.IsNaN(coordinate)||float.IsInfinity(coordinate))throw new ArgumentOutOfRangeException(nameof(coordinate));
            return (int)Math.Round(Math.Max(-HalfSize,Math.Min(HalfSize,coordinate)),MidpointRounding.AwayFromZero);
        }
        public static float Normalized(int coordinate)=>(coordinate+HalfSize+.5f)/TileCount;
        public static int ThreatAt(IReadOnlyList<WorldCamp> camps,int x,int y)
        {
            // Home and the original nine camps anchor stable Voronoi regions. Existing
            // camp levels and coordinates remain authoritative across older saves.
            long best=(long)x*x+(long)y*y;int threat=1;
            for(int i=0;i<Math.Min(LegacyCampCount,camps.Count);i++)
            {
                long dx=x-camps[i].X,dy=y-camps[i].Z,distance=dx*dx+dy*dy;
                if(distance<best){best=distance;threat=camps[i].Level;}
            }
            return threat;
        }
        public static WorldBiome Biome(int threat)=>(WorldBiome)(Math.Max(1,Math.Min(3,threat))-1);
        public static string Name(int threat)=>threat==1?"DRY GRASSLAND":threat==2?"RUST BADLANDS":"ASH WASTES";
        public static uint TileHash(int seed,int x,int y)
        {
            unchecked{uint value=(uint)seed^(uint)x*0x9E3779B9u^(uint)y*0x85EBCA6Bu;value^=value>>16;value*=0x7FEB352Du;value^=value>>15;return value;}
        }
    }
}
