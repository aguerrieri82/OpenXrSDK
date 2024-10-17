using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEditor
{
    public class ContentView
    {
        public string? Title { get; set; }   

        public BaseView? Content { get; set; }


        public IList<ActionView>? Actions { get; set; }  
    }
}
