namespace Notes
{
    public struct NoteMapStats
    {
        public int successHits;
        public int failHits;
        public float accuracy;
        public string mapName;

        public void Record(string p_mapName)
        {
            this.accuracy = (float)successHits / (successHits + failHits);
            this.mapName = p_mapName;
        }
    }
}