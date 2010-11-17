using System;
using System.Windows.Forms;

namespace Servie
{
    class UserControlTab : TabPage
    {
        private UserControl m_Control;
        public UserControl UserControl
        {
            get
            {
                return m_Control;
            }
            set
            {
                m_Control = value;
                Controls.Clear();
                m_Control.Dock = DockStyle.Fill;
                Text = m_Control.Text;
                BackColor = m_Control.BackColor;
                Controls.Add(m_Control);
            }
        }

        public UserControlTab(UserControl control)
        {
            this.UserControl = control;
        }
    }
}
