using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaiduInterop.Interactive
{
    class EnumerableViewer<T>
    {
        public IEnumerable<T> Source { get; private set; }

        public delegate void OnViewItemHandler(int key, T obj);

        public delegate void OnItemSelectedHandler(T obj);

        public OnViewItemHandler OnViewItem { get; set; }

        public OnItemSelectedHandler OnItemSelected { get; set; }

        private void ProcDisplayQueue()
        {
            
        }

        /// <summary>
        /// 在控制台显示集合内容。
        /// </summary>
        public void Show()
        {
            int index = -1;
            int pageTop = Console.CursorTop;
            var viewHeight = Console.WindowHeight - 2;
            var stepMode = false;
            var displayedItems = new List<T>();
            var displayQueue = new Queue<T>();
            Func<bool> showNavigator = () =>
            {
                Console.WriteLine(Prompts.EnumerableViewNavigator, displayedItems.Count);
                NAVIGATION:
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.Enter:
                        stepMode = true;
                        break;
                    case ConsoleKey.Spacebar:
                    case ConsoleKey.PageDown:
                        stepMode = false;
                        //index = 0;
                        break;
                    case ConsoleKey.RightArrow:
                        var selIndex = UI.Input<int>("键入序号", Prompts.Back);
                        if (selIndex == null) goto NAVIGATION;
                        if (selIndex < 0 || selIndex >= displayedItems.Count)
                        {
                            Console.WriteLine(Prompts.NumberOverflow);
                            goto NAVIGATION;
                        }
                        if (OnItemSelected != null)
                            OnItemSelected(displayedItems[selIndex.Value]);
                        return false;
                    case ConsoleKey.Escape:
                        return false;
                    default:
                        goto NAVIGATION;
                }
                pageTop = Console.CursorTop;
                viewHeight = Console.WindowHeight - 2;
                if (Console.BufferHeight - pageTop < viewHeight) Console.Clear();
                return true;
            };
            foreach (var nextObj in Source)
            {
                displayQueue.Enqueue(nextObj);
                displayedItems.Add(nextObj);
                index += 1;
                while (displayQueue.Count > 0)
                {
                    var obj = displayQueue.Dequeue();
                    if (OnViewItem != null) OnViewItem(index, obj);
                    if (stepMode || Console.CursorTop - pageTop >= viewHeight)
                        if (!showNavigator()) return;
                }
            }
            UI.Print(Prompts.EnumerableViewEOF);
            showNavigator();
        }

        public EnumerableViewer(IEnumerable<T> source, OnViewItemHandler onViewItem, OnItemSelectedHandler onItemSelected)
        {
            Source = source;
            OnViewItem = onViewItem;
            OnItemSelected = onItemSelected;
        }
    }
}
