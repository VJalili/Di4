﻿using CSharpTest.Net.Collections;

namespace Polimi.DEIB.VahidJalili.DI4.BasicOperations.IndexFunctions
{
    internal class InfoIndex
    {
        public InfoIndex(BPlusTree<string, int> di4_info)
        {
            _di4_info = di4_info;
        }

        private BPlusTree<string, int> _di4_info { set; get; }
        private AddUpdateValue update = new AddUpdateValue();

        public void AddOrUpdate(string key, int value)
        {
            update.newValue = value;
            _di4_info.AddOrUpdate(key, ref update);
        }
        public int GetValue(string key)
        {
            int rtv = 0;
            _di4_info.TryGetValue(key, out rtv);
            return rtv;
        }


        private struct AddUpdateValue : ICreateOrUpdateValue<string, int>, IRemoveValue<string, int>
        {
            public int oldValue { set; get; }
            public int newValue { set; get; }

            public bool CreateValue(string key, out int value)
            {
                oldValue = 0;
                value = newValue;
                return true;
            }

            public bool UpdateValue(string key, ref int value)
            {
                oldValue = value;
                value = newValue + oldValue;
                return value != oldValue;
            }

            public bool RemoveValue(string key, int value)
            {
                oldValue = value;
                return true;
            }
        }
    }
}
