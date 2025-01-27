using System;
using System.Runtime.InteropServices;


namespace ScanAPP
{

    partial class Form1
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
               IntPtr hWnd,         
               IntPtr hWndInsertAfter, 
               int X,              
               int Y,             
               int cx,              
               int cy,             
               uint uFlags        
           );

        [DllImport("user32.dll", EntryPoint = "#2507")]
        extern static bool SetAutoRotation(bool bEnable);

        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;


        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

