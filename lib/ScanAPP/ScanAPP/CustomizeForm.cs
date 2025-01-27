/*
 * DynaEye 本人確認カメラOCR サンプルプログラム
 * Copyright PFU Limited 2018
 */

using System;
using System.Windows.Forms;

namespace ScanAPP
{
    public partial class CustomizeForm : Form
    {
        private string title;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CustomizeForm()
        {
            InitializeComponent();
        }

        public CustomizeForm(string title, string control)
        {
            this.title = title;
            InitializeComponent();
            this.titleLabel.Text = title;
            // スキャンモードの場合、枠内の文言は表示しない
            // if (Const.CAMERA_MODE_SCAN.Equals(control))
            // {
            // this.msgToFrameLabel.Visible = false;
            // }
        }

        private void CustomizeForm_Load(object sender, EventArgs e)
        {

        }

        private void textLabel_Click(object sender, EventArgs e)
        {

        }

        private void msgToFrameLabel_Click(object sender, EventArgs e)
        {

        }
    }
}
