using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebBanHangOnline.Models;

namespace WebBanHangOnline.Common
{
    public class SettingHelper
    {
        private static ApplicationDbContext db = new ApplicationDbContext();

        public static string GetValue(string key)
        {
            using (var db = new ApplicationDbContext())
            {
                var item = db.SystemSettings.SingleOrDefault(x => x.SettingKey == key);
                return item?.SettingValue ?? "";
            }
        }

    }
}