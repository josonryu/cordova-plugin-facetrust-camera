/*
 * DynaEye 本人確認カメラOCR サンプルプログラム
 * Copyright PFU Limited 2018
 */

namespace ScanAPP
{
    partial class CustomizeForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム　デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.titleBackLabel = new System.Windows.Forms.Label();
            this.titleLabel = new System.Windows.Forms.Label();
            this.msgToFrameLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // titleBackLabel
            // 
            this.titleBackLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.titleBackLabel.BackColor = System.Drawing.Color.Black;
            this.titleBackLabel.Location = new System.Drawing.Point(0, 0);
            this.titleBackLabel.Name = "titleBackLabel";
            this.titleBackLabel.Size = new System.Drawing.Size(1265, 46);
            this.titleBackLabel.TabIndex = 0;
            // 
            // titleLabel
            // 
            this.titleLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.titleLabel.BackColor = System.Drawing.Color.Black;
            this.titleLabel.Font = new System.Drawing.Font("HGPｺﾞｼｯｸE", 20F);
            this.titleLabel.ForeColor = System.Drawing.Color.Transparent;
            this.titleLabel.Location = new System.Drawing.Point(0, 0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.titleLabel.Size = new System.Drawing.Size(1263, 46);
            this.titleLabel.TabIndex = 1;
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // msgToFrameLabel
            // 
            this.msgToFrameLabel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.msgToFrameLabel.BackColor = System.Drawing.Color.Transparent;
            this.msgToFrameLabel.Font = new System.Drawing.Font("HGPｺﾞｼｯｸE", 20F);
            this.msgToFrameLabel.ForeColor = System.Drawing.Color.Transparent;
            this.msgToFrameLabel.Location = new System.Drawing.Point(5, 750);
            this.msgToFrameLabel.Name = "msgToFrameLabel";
            this.msgToFrameLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.msgToFrameLabel.Size = new System.Drawing.Size(1258, 42);
            this.msgToFrameLabel.TabIndex = 3;
            this.msgToFrameLabel.Text = "枠内に書面をあわせてください";
            this.msgToFrameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.msgToFrameLabel.Click += new System.EventHandler(this.msgToFrameLabel_Click);
            // 
            // CustomizeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.CadetBlue;
            this.ClientSize = new System.Drawing.Size(1264, 681);
            this.Controls.Add(this.msgToFrameLabel);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.titleBackLabel);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Name = "CustomizeForm";
            this.Opacity = 0.8D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "CustomForm";
            this.TransparencyKey = System.Drawing.Color.CadetBlue;
            this.Load += new System.EventHandler(this.CustomizeForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label titleBackLabel;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label msgToFrameLabel;
    }
}