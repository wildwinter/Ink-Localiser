using Ink.Parsed;

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

            List<Text> textObjects = _story.FindAll<Text>();
            int lastLineNumber = -1;
            foreach(var text in textObjects)
            {
                // Just a newline? Ignore.
                if (text.text.Trim()=="")
                    continue;
                
                // Checking it's a tag. Is there a StartTag earlier in the parent content?
                int inTag = 0;
                foreach (var sibling in text.parent.content) {
                    if (sibling==text) {
                        break;
                    }
                    if (sibling is Tag) {
                        var tag = (Tag)sibling;
                        if (tag.isStart)
                            inTag++;
                        else
                            inTag--;
                    }
                }

                if (inTag>0)
                    continue;

                // Is this inside a variable assignment? In which case we can't do anything with that.
                if (text.parent is VariableAssignment) {
                    continue;
                }

                // More than one tect chunk on a line? We only deal with individual lines of stuff.
                if (lastLineNumber == text.debugMetadata.startLineNumber) {
                    Console.Error.WriteLine($"Error in line {lastLineNumber} - two chunks of text when localiser can only work with one per line.");
                    return false;
                }
                lastLineNumber = text.debugMetadata.startLineNumber;
                    
                Console.WriteLine("["+text.debugMetadata.startLineNumber+"] "+text.text+" - "+text.parent.parent);

                
            }
            return true;
        }
    }
}