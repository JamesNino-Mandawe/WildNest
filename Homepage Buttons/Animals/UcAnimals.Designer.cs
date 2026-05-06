namespace Project
{
    partial class UcAnimals
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pnlHero = new Panel();
            tlpViewSwitcher = new TableLayoutPanel();
            btnAllResidents = new Button();
            btnSpotlight = new Button();
            lblHeroSub = new Label();
            lblHeroTitle = new Label();
            lblLocation = new Label();
            pnlStatusBar = new Panel();
            tlpStatusBar = new TableLayoutPanel();
            lblCabinStatus = new Label();
            lblSafariTimer = new Label();
            lblAnimalCount = new Label();
            pnlContentArea = new Panel();
            pnlQueue = new Panel();
            lblQueueCount = new Label();
            pbQueueProgress = new ProgressBar();
            lblQueueSub = new Label();
            lblQueueTitle = new Label();
            flpQueueList = new FlowLayoutPanel();
            pnlPhotoArea = new Panel();
            pnlActionBar = new Panel();
            btnSpotlightNext = new Button();
            lblNextAnimalName = new Label();
            lblUpNext = new Label();
            picNextAnimal = new PictureBox();
            picAnimalPhoto = new Panel();
            flpTags = new FlowLayoutPanel();
            pnlFactBox = new Panel();
            lblDescription = new Label();
            flpStatPills = new FlowLayoutPanel();
            lblSpecies = new Label();
            lblAnimalName = new Label();
            lblZoneBadge = new Label();
            lblConservationStatus = new Label();
            lblAnimalCounter = new Label();
            pnlHero.SuspendLayout();
            tlpViewSwitcher.SuspendLayout();
            pnlStatusBar.SuspendLayout();
            tlpStatusBar.SuspendLayout();
            pnlContentArea.SuspendLayout();
            pnlQueue.SuspendLayout();
            pnlPhotoArea.SuspendLayout();
            pnlActionBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picNextAnimal).BeginInit();
            picAnimalPhoto.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHero
            // 
            pnlHero.Controls.Add(tlpViewSwitcher);
            pnlHero.Controls.Add(lblHeroSub);
            pnlHero.Controls.Add(lblHeroTitle);
            pnlHero.Controls.Add(lblLocation);
            pnlHero.Dock = DockStyle.Top;
            pnlHero.Location = new Point(0, 0);
            pnlHero.Name = "pnlHero";
            pnlHero.Size = new Size(1262, 320);
            pnlHero.TabIndex = 3;
            // 
            // tlpViewSwitcher
            // 
            tlpViewSwitcher.ColumnCount = 2;
            tlpViewSwitcher.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpViewSwitcher.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpViewSwitcher.Controls.Add(btnAllResidents, 1, 0);
            tlpViewSwitcher.Controls.Add(btnSpotlight, 0, 0);
            tlpViewSwitcher.Location = new Point(382, 147);
            tlpViewSwitcher.Name = "tlpViewSwitcher";
            tlpViewSwitcher.RowCount = 1;
            tlpViewSwitcher.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpViewSwitcher.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tlpViewSwitcher.Size = new Size(490, 92);
            tlpViewSwitcher.TabIndex = 5;
            // 
            // btnAllResidents
            // 
            btnAllResidents.Location = new Point(248, 3);
            btnAllResidents.Name = "btnAllResidents";
            btnAllResidents.Size = new Size(82, 29);
            btnAllResidents.TabIndex = 1;
            btnAllResidents.Text = "button2";
            btnAllResidents.UseVisualStyleBackColor = true;
            // 
            // btnSpotlight
            // 
            btnSpotlight.Location = new Point(3, 3);
            btnSpotlight.Name = "btnSpotlight";
            btnSpotlight.Size = new Size(82, 29);
            btnSpotlight.TabIndex = 0;
            btnSpotlight.Text = "button1";
            btnSpotlight.UseVisualStyleBackColor = true;
            // 
            // lblHeroSub
            // 
            lblHeroSub.AutoSize = true;
            lblHeroSub.Location = new Point(208, 105);
            lblHeroSub.Name = "lblHeroSub";
            lblHeroSub.Size = new Size(887, 20);
            lblHeroSub.TabIndex = 4;
            lblHeroSub.Text = "35 remarkable species roaming freely across 170 hectares of restored sanctuary — each with a name, a story, and a dedicated ranger.";
            // 
            // lblHeroTitle
            // 
            lblHeroTitle.AutoSize = true;
            lblHeroTitle.Location = new Point(502, 69);
            lblHeroTitle.Name = "lblHeroTitle";
            lblHeroTitle.Size = new Size(170, 20);
            lblHeroTitle.TabIndex = 3;
            lblHeroTitle.Text = "Meet the Wild Residents";
            // 
            // lblLocation
            // 
            lblLocation.AutoSize = true;
            lblLocation.Location = new Point(487, 31);
            lblLocation.Name = "lblLocation";
            lblLocation.Size = new Size(216, 20);
            lblLocation.TabIndex = 2;
            lblLocation.Text = "CARMEN, CEBU — PHILIPPINES";
            // 
            // pnlStatusBar
            // 
            pnlStatusBar.BackColor = Color.DarkOliveGreen;
            pnlStatusBar.Controls.Add(tlpStatusBar);
            pnlStatusBar.Dock = DockStyle.Top;
            pnlStatusBar.Location = new Point(0, 320);
            pnlStatusBar.Name = "pnlStatusBar";
            pnlStatusBar.Size = new Size(1262, 60);
            pnlStatusBar.TabIndex = 4;
            // 
            // tlpStatusBar
            // 
            tlpStatusBar.ColumnCount = 3;
            tlpStatusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            tlpStatusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            tlpStatusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            tlpStatusBar.Controls.Add(lblCabinStatus, 0, 0);
            tlpStatusBar.Controls.Add(lblSafariTimer, 1, 0);
            tlpStatusBar.Controls.Add(lblAnimalCount, 2, 0);
            tlpStatusBar.Dock = DockStyle.Fill;
            tlpStatusBar.Location = new Point(0, 0);
            tlpStatusBar.Name = "tlpStatusBar";
            tlpStatusBar.RowCount = 1;
            tlpStatusBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpStatusBar.Size = new Size(1262, 60);
            tlpStatusBar.TabIndex = 0;
            // 
            // lblCabinStatus
            // 
            lblCabinStatus.AutoSize = true;
            lblCabinStatus.Dock = DockStyle.Fill;
            lblCabinStatus.Font = new Font("Arial Rounded MT Bold", 12F);
            lblCabinStatus.Location = new Point(3, 0);
            lblCabinStatus.Name = "lblCabinStatus";
            lblCabinStatus.Size = new Size(414, 60);
            lblCabinStatus.TabIndex = 0;
            lblCabinStatus.Text = "NA";
            lblCabinStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSafariTimer
            // 
            lblSafariTimer.AutoSize = true;
            lblSafariTimer.Dock = DockStyle.Fill;
            lblSafariTimer.Font = new Font("Arial Rounded MT Bold", 12F);
            lblSafariTimer.Location = new Point(423, 0);
            lblSafariTimer.Name = "lblSafariTimer";
            lblSafariTimer.Size = new Size(414, 60);
            lblSafariTimer.TabIndex = 1;
            lblSafariTimer.Text = "NA";
            lblSafariTimer.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblAnimalCount
            // 
            lblAnimalCount.AutoSize = true;
            lblAnimalCount.Dock = DockStyle.Fill;
            lblAnimalCount.Font = new Font("Arial Rounded MT Bold", 12F);
            lblAnimalCount.Location = new Point(843, 0);
            lblAnimalCount.Name = "lblAnimalCount";
            lblAnimalCount.Size = new Size(416, 60);
            lblAnimalCount.TabIndex = 2;
            lblAnimalCount.Text = "NA";
            lblAnimalCount.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnlContentArea
            // 
            pnlContentArea.Controls.Add(pnlQueue);
            pnlContentArea.Controls.Add(pnlPhotoArea);
            pnlContentArea.Dock = DockStyle.Fill;
            pnlContentArea.Location = new Point(0, 380);
            pnlContentArea.Name = "pnlContentArea";
            pnlContentArea.Size = new Size(1262, 771);
            pnlContentArea.TabIndex = 5;
            // 
            // pnlQueue
            // 
            pnlQueue.Controls.Add(lblQueueCount);
            pnlQueue.Controls.Add(pbQueueProgress);
            pnlQueue.Controls.Add(lblQueueSub);
            pnlQueue.Controls.Add(lblQueueTitle);
            pnlQueue.Controls.Add(flpQueueList);
            pnlQueue.Dock = DockStyle.Right;
            pnlQueue.Location = new Point(872, 0);
            pnlQueue.Name = "pnlQueue";
            pnlQueue.Size = new Size(390, 771);
            pnlQueue.TabIndex = 2;
            // 
            // lblQueueCount
            // 
            lblQueueCount.AutoSize = true;
            lblQueueCount.Location = new Point(336, 78);
            lblQueueCount.Name = "lblQueueCount";
            lblQueueCount.Size = new Size(39, 20);
            lblQueueCount.TabIndex = 3;
            lblQueueCount.Text = "1/35";
            // 
            // pbQueueProgress
            // 
            pbQueueProgress.Location = new Point(23, 78);
            pbQueueProgress.Name = "pbQueueProgress";
            pbQueueProgress.Size = new Size(307, 20);
            pbQueueProgress.TabIndex = 2;
            // 
            // lblQueueSub
            // 
            lblQueueSub.AutoSize = true;
            lblQueueSub.Location = new Point(23, 44);
            lblQueueSub.Name = "lblQueueSub";
            lblQueueSub.Size = new Size(243, 20);
            lblQueueSub.TabIndex = 1;
            lblQueueSub.Text = "All 35 residents — cycling endlessly";
            // 
            // lblQueueTitle
            // 
            lblQueueTitle.AutoSize = true;
            lblQueueTitle.Location = new Point(23, 15);
            lblQueueTitle.Name = "lblQueueTitle";
            lblQueueTitle.Size = new Size(115, 20);
            lblQueueTitle.TabIndex = 0;
            lblQueueTitle.Text = "ANIMAL QUEUE";
            // 
            // flpQueueList
            // 
            flpQueueList.Dock = DockStyle.Fill;
            flpQueueList.FlowDirection = FlowDirection.TopDown;
            flpQueueList.Location = new Point(0, 0);
            flpQueueList.Name = "flpQueueList";
            flpQueueList.Size = new Size(390, 771);
            flpQueueList.TabIndex = 4;
            // 
            // pnlPhotoArea
            // 
            pnlPhotoArea.Controls.Add(pnlActionBar);
            pnlPhotoArea.Controls.Add(picAnimalPhoto);
            pnlPhotoArea.Dock = DockStyle.Fill;
            pnlPhotoArea.Location = new Point(0, 0);
            pnlPhotoArea.Name = "pnlPhotoArea";
            pnlPhotoArea.Size = new Size(1262, 771);
            pnlPhotoArea.TabIndex = 0;
            // 
            // pnlActionBar
            // 
            pnlActionBar.Controls.Add(btnSpotlightNext);
            pnlActionBar.Controls.Add(lblNextAnimalName);
            pnlActionBar.Controls.Add(lblUpNext);
            pnlActionBar.Controls.Add(picNextAnimal);
            pnlActionBar.Dock = DockStyle.Bottom;
            pnlActionBar.Location = new Point(0, 671);
            pnlActionBar.Name = "pnlActionBar";
            pnlActionBar.Size = new Size(882, 100);
            pnlActionBar.TabIndex = 0;
            // 
            // btnSpotlightNext
            // 
            btnSpotlightNext.Location = new Point(532, 23);
            btnSpotlightNext.Name = "btnSpotlightNext";
            btnSpotlightNext.Size = new Size(290, 54);
            btnSpotlightNext.TabIndex = 3;
            btnSpotlightNext.Text = "Spotlight Next Animal →";
            btnSpotlightNext.UseVisualStyleBackColor = true;
            // 
            // lblNextAnimalName
            // 
            lblNextAnimalName.AutoSize = true;
            lblNextAnimalName.Location = new Point(114, 43);
            lblNextAnimalName.Name = "lblNextAnimalName";
            lblNextAnimalName.Size = new Size(95, 20);
            lblNextAnimalName.TabIndex = 2;
            lblNextAnimalName.Text = "animal name";
            // 
            // lblUpNext
            // 
            lblUpNext.AutoSize = true;
            lblUpNext.Location = new Point(114, 23);
            lblUpNext.Name = "lblUpNext";
            lblUpNext.Size = new Size(67, 20);
            lblUpNext.TabIndex = 1;
            lblUpNext.Text = "UP NEXT";
            // 
            // picNextAnimal
            // 
            picNextAnimal.Location = new Point(20, 14);
            picNextAnimal.Name = "picNextAnimal";
            picNextAnimal.Size = new Size(70, 70);
            picNextAnimal.TabIndex = 0;
            picNextAnimal.TabStop = false;
            // 
            // picAnimalPhoto
            // 
            picAnimalPhoto.Controls.Add(flpTags);
            picAnimalPhoto.Controls.Add(pnlFactBox);
            picAnimalPhoto.Controls.Add(lblDescription);
            picAnimalPhoto.Controls.Add(flpStatPills);
            picAnimalPhoto.Controls.Add(lblSpecies);
            picAnimalPhoto.Controls.Add(lblAnimalName);
            picAnimalPhoto.Controls.Add(lblZoneBadge);
            picAnimalPhoto.Controls.Add(lblConservationStatus);
            picAnimalPhoto.Controls.Add(lblAnimalCounter);
            picAnimalPhoto.Dock = DockStyle.Fill;
            picAnimalPhoto.Location = new Point(0, 0);
            picAnimalPhoto.Name = "picAnimalPhoto";
            picAnimalPhoto.Size = new Size(1262, 771);
            picAnimalPhoto.TabIndex = 0;
            // 
            // flpTags
            // 
            flpTags.Location = new Point(81, 634);
            flpTags.Name = "flpTags";
            flpTags.Size = new Size(336, 33);
            flpTags.TabIndex = 8;
            // 
            // pnlFactBox
            // 
            pnlFactBox.Location = new Point(87, 363);
            pnlFactBox.Name = "pnlFactBox";
            pnlFactBox.Size = new Size(666, 149);
            pnlFactBox.TabIndex = 7;
            // 
            // lblDescription
            // 
            lblDescription.AutoSize = true;
            lblDescription.Location = new Point(91, 308);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(42, 20);
            lblDescription.TabIndex = 6;
            lblDescription.Text = "label";
            // 
            // flpStatPills
            // 
            flpStatPills.Location = new Point(90, 186);
            flpStatPills.Name = "flpStatPills";
            flpStatPills.Size = new Size(512, 90);
            flpStatPills.TabIndex = 5;
            // 
            // lblSpecies
            // 
            lblSpecies.AutoSize = true;
            lblSpecies.Location = new Point(88, 152);
            lblSpecies.Name = "lblSpecies";
            lblSpecies.Size = new Size(42, 20);
            lblSpecies.TabIndex = 4;
            lblSpecies.Text = "label";
            // 
            // lblAnimalName
            // 
            lblAnimalName.AutoSize = true;
            lblAnimalName.Location = new Point(88, 122);
            lblAnimalName.Name = "lblAnimalName";
            lblAnimalName.Size = new Size(42, 20);
            lblAnimalName.TabIndex = 3;
            lblAnimalName.Text = "label";
            // 
            // lblZoneBadge
            // 
            lblZoneBadge.AutoSize = true;
            lblZoneBadge.Location = new Point(71, 89);
            lblZoneBadge.Name = "lblZoneBadge";
            lblZoneBadge.Size = new Size(151, 20);
            lblZoneBadge.TabIndex = 2;
            lblZoneBadge.Text = "● GOLDEN SAVANNA";
            // 
            // lblConservationStatus
            // 
            lblConservationStatus.AutoSize = true;
            lblConservationStatus.Location = new Point(757, 15);
            lblConservationStatus.Name = "lblConservationStatus";
            lblConservationStatus.Size = new Size(101, 20);
            lblConservationStatus.TabIndex = 1;
            lblConservationStatus.Text = "Least Concern";
            // 
            // lblAnimalCounter
            // 
            lblAnimalCounter.AutoSize = true;
            lblAnimalCounter.Location = new Point(20, 15);
            lblAnimalCounter.Name = "lblAnimalCounter";
            lblAnimalCounter.Size = new Size(118, 20);
            lblAnimalCounter.TabIndex = 0;
            lblAnimalCounter.Text = "ANIMAL 1 OF 35";
            // 
            // UcAnimals
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(pnlContentArea);
            Controls.Add(pnlStatusBar);
            Controls.Add(pnlHero);
            Name = "UcAnimals";
            Size = new Size(1262, 1151);
            pnlHero.ResumeLayout(false);
            pnlHero.PerformLayout();
            tlpViewSwitcher.ResumeLayout(false);
            pnlStatusBar.ResumeLayout(false);
            tlpStatusBar.ResumeLayout(false);
            tlpStatusBar.PerformLayout();
            pnlContentArea.ResumeLayout(false);
            pnlQueue.ResumeLayout(false);
            pnlQueue.PerformLayout();
            pnlPhotoArea.ResumeLayout(false);
            pnlActionBar.ResumeLayout(false);
            pnlActionBar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picNextAnimal).EndInit();
            picAnimalPhoto.ResumeLayout(false);
            picAnimalPhoto.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlHero;
        private Label lblHeroSub;
        private Label lblHeroTitle;
        private Label lblLocation;
        private Panel pnlStatusBar;
        private TableLayoutPanel tlpStatusBar;
        private Label lblCabinStatus;
        private Label lblSafariTimer;
        private Label lblAnimalCount;

        private Panel pnlContentArea;
        private Panel pnlQueue;
        private Panel pnlActionBar;
        private Button btnSpotlightNext;
        private Label lblNextAnimalName;
        private Label lblUpNext;
        private PictureBox picNextAnimal;
        private FlowLayoutPanel flpQueueList;
        private Label lblQueueCount;
        private ProgressBar pbQueueProgress;
        private Label lblQueueSub;
        private Label lblQueueTitle;
        private TableLayoutPanel tlpViewSwitcher;
        private Button btnAllResidents;
        private Button btnSpotlight;
        private Panel pnlPhotoArea;
        private Panel picAnimalPhoto;
        private FlowLayoutPanel flpTags;
        private Panel pnlFactBox;
        private Label lblDescription;
        private FlowLayoutPanel flpStatPills;
        private Label lblSpecies;
        private Label lblAnimalName;
        private Label lblZoneBadge;
        private Label lblConservationStatus;
        private Label lblAnimalCounter;
    }
}