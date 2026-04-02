using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace WpfImageEditor
{
    public class FilterHistory
    {
        private readonly Stack<BitmapSource> _stack = new();

        public bool CanUndo => _stack.Count > 1;

        public void Initialize(BitmapSource original)
        {
            _stack.Clear();
            _stack.Push(original);
        }

        public BitmapSource Current => _stack.Peek();

        public void Push(BitmapSource result) => _stack.Push(result);

        public BitmapSource Undo()
        {
            if (_stack.Count > 1) _stack.Pop();
            return _stack.Peek();
        }

        public BitmapSource Reset()
        {
            while (_stack.Count > 1) _stack.Pop();
            return _stack.Peek();
        }

        public int HistoryDepth => _stack.Count;
    }
}
