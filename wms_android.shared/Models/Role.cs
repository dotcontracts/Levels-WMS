using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wms_android.shared.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        // Permission flags
        public bool CanConfirm { get; set; } = false;
        public bool CanDispatch { get; set; } = false;
    }

}
