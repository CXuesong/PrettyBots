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

        public delegate void OnViewObjectHandler(int key, T obj);

        public delegate void OnObjectSelectedHandler(T obj);

        public OnViewObjectHandler OnViewObject { get; set; }

        public OnObjectSelectedHandler OnObjectSelected { get; set; }

        /// <summary>
        /// 在控制台显示集合内容。
        /// </summary>
        public void Show()
        {
            int displayedItemsCount = 0;
            int pageTop = Console.CursorTop;
            var viewHeight = Console.WindowHeight - 2;
            var stepMode = false;
            var displayedItems = new List<T>();
            var displayQueue = new Queue<T>();
            foreach (var nextObj in Source)
            {
                displayQueue.Enqueue(nextObj);
                displayedItems.Add(nextObj);
                displayedItemsCount += 1;
                while (displayQueue.Count > 0)
                {
                    var obj = displayQueue.Dequeue();
                    if (OnViewObject != null) OnViewObject(displayedItemsCount, obj);
                    if (stepMode || Console.CursorTop - pageTop + 2 >= viewHeight)
                    {
                        //PAUSE
                        Console.WriteLine(Prompts.EnumerableViewNavigator, displayedItemsCount);
                        NAVIGATION:
                        switch (Console.ReadKey(true).Key)
                        {
                            case ConsoleKey.Enter:
                                stepMode = true;
                                continue;
                            case ConsoleKey.Spacebar:
                            case ConsoleKey.PageDown:
                                stepMode = false;
                                pageTop = Console.CursorTop;
                                //index = 0;
                                continue;
                            case ConsoleKey.RightArrow:
                                var selIndex = UI.Input<int>("键入序号", Prompts.Back);
                                if (selIndex == null) goto NAVIGATION;
                                if (selIndex < 0 || selIndex >= displayedItems.Count)
                                {
                                    Console.WriteLine(Prompts.NumberOverflow);
                                    goto NAVIGATION;
                                }
                                if (OnObjectSelected != null) 
                                    OnObjectSelected(displayedItems[selIndex.Value]);
                                return;
                            case ConsoleKey.Escape:
                                return;
                        }
                    }
                }
            }
        }

        public EnumerableViewer(IEnumerable<T> source, OnViewObjectHandler onViewObject, OnObjectSelectedHandler onObjectSelected)
        {
            Source = source;
            OnViewObject = onViewObject;
            OnObjectSelected = onObjectSelected;
        }
    }
}
