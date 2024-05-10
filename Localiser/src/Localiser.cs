namespace InkLocaliser
{
    public class Localiser
    {
        private Ink.Parsed.Story _story;

        public Localiser(Ink.Parsed.Story story)
        {
            _story = story;
        }

        public bool Run() {
            return true;
        }
    }
}