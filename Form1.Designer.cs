namespace ConnPostSQL
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnConn = new System.Windows.Forms.Button();
            this.GvwData = new System.Windows.Forms.DataGridView();
            this.label1 = new System.Windows.Forms.Label();
            this.tbHost = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tbPort = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbUser = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbPass = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbDataBaseName = new System.Windows.Forms.TextBox();
            this.btnNewLines = new System.Windows.Forms.Button();
            this.btnBuildInterTable = new System.Windows.Forms.Button();
            this.btnAssignID = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxStartPoint = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBoxEndPoint = new System.Windows.Forms.TextBox();
            this.btnGetIDRange = new System.Windows.Forms.Button();
            this.richTextBoxRange = new System.Windows.Forms.RichTextBox();
            this.btnGetSPath = new System.Windows.Forms.Button();
            this.btnBreakLines = new System.Windows.Forms.Button();
            this.btnIntersect = new System.Windows.Forms.Button();
            this.btnAdjactList = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.txtLayerName = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtPointCount = new System.Windows.Forms.TextBox();
            this.btnSubGraph = new System.Windows.Forms.Button();
            this.btnEntryPoints = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnMetisData = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnUpdateVertexAdj = new System.Windows.Forms.Button();
            this.btnPolygonize = new System.Windows.Forms.Button();
            this.btnLineAdjaction = new System.Windows.Forms.Button();
            this.cbxAlgorithm = new System.Windows.Forms.ComboBox();
            this.btnClassical = new System.Windows.Forms.Button();
            this.btnHierarchyAlgorthm = new System.Windows.Forms.Button();
            this.btnBDAlgorithm = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.cbxMetisAlgorithm = new System.Windows.Forms.ComboBox();
            this.txtPartNum = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.cbxBdAlgorithms = new System.Windows.Forms.ComboBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label14 = new System.Windows.Forms.Label();
            this.txtCostTime = new System.Windows.Forms.TextBox();
            this.btnMETISTest = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.GvwData)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnConn
            // 
            this.btnConn.Location = new System.Drawing.Point(272, 108);
            this.btnConn.Name = "btnConn";
            this.btnConn.Size = new System.Drawing.Size(75, 36);
            this.btnConn.TabIndex = 0;
            this.btnConn.Text = "连接";
            this.btnConn.UseVisualStyleBackColor = true;
            this.btnConn.Click += new System.EventHandler(this.btnConn_Click);
            // 
            // GvwData
            // 
            this.GvwData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.GvwData.Location = new System.Drawing.Point(12, 214);
            this.GvwData.Name = "GvwData";
            this.GvwData.RowTemplate.Height = 23;
            this.GvwData.Size = new System.Drawing.Size(335, 202);
            this.GvwData.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "Server:";
            // 
            // tbHost
            // 
            this.tbHost.Location = new System.Drawing.Point(71, 14);
            this.tbHost.Name = "tbHost";
            this.tbHost.Size = new System.Drawing.Size(100, 21);
            this.tbHost.TabIndex = 4;
            this.tbHost.Text = "localhost";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(194, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 12);
            this.label2.TabIndex = 3;
            this.label2.Text = "Port:";
            // 
            // tbPort
            // 
            this.tbPort.Location = new System.Drawing.Point(247, 13);
            this.tbPort.Name = "tbPort";
            this.tbPort.Size = new System.Drawing.Size(100, 21);
            this.tbPort.TabIndex = 4;
            this.tbPort.Text = "5432";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 68);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 12);
            this.label3.TabIndex = 3;
            this.label3.Text = "User:";
            // 
            // tbUser
            // 
            this.tbUser.Location = new System.Drawing.Point(71, 63);
            this.tbUser.Name = "tbUser";
            this.tbUser.Size = new System.Drawing.Size(100, 21);
            this.tbUser.TabIndex = 4;
            this.tbUser.Text = "postgres";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(188, 68);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 12);
            this.label4.TabIndex = 3;
            this.label4.Text = "Password:";
            // 
            // tbPass
            // 
            this.tbPass.Location = new System.Drawing.Point(247, 63);
            this.tbPass.Name = "tbPass";
            this.tbPass.PasswordChar = '*';
            this.tbPass.Size = new System.Drawing.Size(100, 21);
            this.tbPass.TabIndex = 4;
            this.tbPass.Text = "ronald";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 120);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(59, 12);
            this.label5.TabIndex = 3;
            this.label5.Text = "DataBase:";
            // 
            // tbDataBaseName
            // 
            this.tbDataBaseName.Location = new System.Drawing.Point(83, 117);
            this.tbDataBaseName.Name = "tbDataBaseName";
            this.tbDataBaseName.Size = new System.Drawing.Size(100, 21);
            this.tbDataBaseName.TabIndex = 4;
            this.tbDataBaseName.Text = "postgis";
            // 
            // btnNewLines
            // 
            this.btnNewLines.Location = new System.Drawing.Point(391, 249);
            this.btnNewLines.Name = "btnNewLines";
            this.btnNewLines.Size = new System.Drawing.Size(86, 42);
            this.btnNewLines.TabIndex = 9;
            this.btnNewLines.Text = "构建新线段";
            this.btnNewLines.UseVisualStyleBackColor = true;
            this.btnNewLines.Click += new System.EventHandler(this.btnNewLines_Click);
            // 
            // btnBuildInterTable
            // 
            this.btnBuildInterTable.Location = new System.Drawing.Point(391, 171);
            this.btnBuildInterTable.Name = "btnBuildInterTable";
            this.btnBuildInterTable.Size = new System.Drawing.Size(86, 41);
            this.btnBuildInterTable.TabIndex = 10;
            this.btnBuildInterTable.Text = "构建交点表";
            this.btnBuildInterTable.UseVisualStyleBackColor = true;
            this.btnBuildInterTable.Click += new System.EventHandler(this.btnBuildInterTable_Click);
            // 
            // btnAssignID
            // 
            this.btnAssignID.Location = new System.Drawing.Point(391, 326);
            this.btnAssignID.Name = "btnAssignID";
            this.btnAssignID.Size = new System.Drawing.Size(86, 41);
            this.btnAssignID.TabIndex = 12;
            this.btnAssignID.Text = "分配ID";
            this.btnAssignID.UseVisualStyleBackColor = true;
            this.btnAssignID.Click += new System.EventHandler(this.btnAssignID_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(529, 132);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 12);
            this.label6.TabIndex = 13;
            this.label6.Text = "起始点ID：";
            // 
            // textBoxStartPoint
            // 
            this.textBoxStartPoint.Location = new System.Drawing.Point(531, 160);
            this.textBoxStartPoint.Name = "textBoxStartPoint";
            this.textBoxStartPoint.Size = new System.Drawing.Size(76, 21);
            this.textBoxStartPoint.TabIndex = 14;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(663, 132);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(65, 12);
            this.label7.TabIndex = 13;
            this.label7.Text = "终止点ID：";
            // 
            // textBoxEndPoint
            // 
            this.textBoxEndPoint.Location = new System.Drawing.Point(663, 160);
            this.textBoxEndPoint.Name = "textBoxEndPoint";
            this.textBoxEndPoint.Size = new System.Drawing.Size(76, 21);
            this.textBoxEndPoint.TabIndex = 14;
            // 
            // btnGetIDRange
            // 
            this.btnGetIDRange.Location = new System.Drawing.Point(531, 17);
            this.btnGetIDRange.Name = "btnGetIDRange";
            this.btnGetIDRange.Size = new System.Drawing.Size(86, 44);
            this.btnGetIDRange.TabIndex = 15;
            this.btnGetIDRange.Text = "获取ID范围";
            this.btnGetIDRange.UseVisualStyleBackColor = true;
            this.btnGetIDRange.Click += new System.EventHandler(this.btnGetIDRange_Click);
            // 
            // richTextBoxRange
            // 
            this.richTextBoxRange.Location = new System.Drawing.Point(647, 17);
            this.richTextBoxRange.Name = "richTextBoxRange";
            this.richTextBoxRange.Size = new System.Drawing.Size(173, 58);
            this.richTextBoxRange.TabIndex = 16;
            this.richTextBoxRange.Text = "";
            // 
            // btnGetSPath
            // 
            this.btnGetSPath.Location = new System.Drawing.Point(769, 155);
            this.btnGetSPath.Name = "btnGetSPath";
            this.btnGetSPath.Size = new System.Drawing.Size(86, 42);
            this.btnGetSPath.TabIndex = 17;
            this.btnGetSPath.Text = "最短路径";
            this.btnGetSPath.UseVisualStyleBackColor = true;
            this.btnGetSPath.Click += new System.EventHandler(this.btnGetSPath_Click);
            // 
            // btnBreakLines
            // 
            this.btnBreakLines.Location = new System.Drawing.Point(391, 19);
            this.btnBreakLines.Name = "btnBreakLines";
            this.btnBreakLines.Size = new System.Drawing.Size(86, 43);
            this.btnBreakLines.TabIndex = 20;
            this.btnBreakLines.Text = "拆分线段";
            this.btnBreakLines.UseVisualStyleBackColor = true;
            this.btnBreakLines.Click += new System.EventHandler(this.btnBreakLines_Click);
            // 
            // btnIntersect
            // 
            this.btnIntersect.Location = new System.Drawing.Point(391, 99);
            this.btnIntersect.Name = "btnIntersect";
            this.btnIntersect.Size = new System.Drawing.Size(86, 43);
            this.btnIntersect.TabIndex = 20;
            this.btnIntersect.Text = "求交点";
            this.btnIntersect.UseVisualStyleBackColor = true;
            this.btnIntersect.Click += new System.EventHandler(this.btnIntersect_Click);
            // 
            // btnAdjactList
            // 
            this.btnAdjactList.Location = new System.Drawing.Point(873, 19);
            this.btnAdjactList.Name = "btnAdjactList";
            this.btnAdjactList.Size = new System.Drawing.Size(75, 42);
            this.btnAdjactList.TabIndex = 21;
            this.btnAdjactList.Text = "邻接表";
            this.btnAdjactList.UseVisualStyleBackColor = true;
            this.btnAdjactList.Click += new System.EventHandler(this.btnAdjactList_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(16, 170);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(65, 12);
            this.label8.TabIndex = 22;
            this.label8.Text = "图层名称：";
            // 
            // txtLayerName
            // 
            this.txtLayerName.Location = new System.Drawing.Point(83, 166);
            this.txtLayerName.Name = "txtLayerName";
            this.txtLayerName.Size = new System.Drawing.Size(88, 21);
            this.txtLayerName.TabIndex = 23;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(184, 171);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(77, 12);
            this.label9.TabIndex = 24;
            this.label9.Text = "存储点总数：";
            // 
            // txtPointCount
            // 
            this.txtPointCount.Location = new System.Drawing.Point(255, 167);
            this.txtPointCount.Name = "txtPointCount";
            this.txtPointCount.Size = new System.Drawing.Size(88, 21);
            this.txtPointCount.TabIndex = 25;
            // 
            // btnSubGraph
            // 
            this.btnSubGraph.Location = new System.Drawing.Point(12, 17);
            this.btnSubGraph.Name = "btnSubGraph";
            this.btnSubGraph.Size = new System.Drawing.Size(75, 38);
            this.btnSubGraph.TabIndex = 26;
            this.btnSubGraph.Text = "分区";
            this.btnSubGraph.UseVisualStyleBackColor = true;
            this.btnSubGraph.Click += new System.EventHandler(this.btnSubGraph_Click);
            // 
            // btnEntryPoints
            // 
            this.btnEntryPoints.Location = new System.Drawing.Point(113, 19);
            this.btnEntryPoints.Name = "btnEntryPoints";
            this.btnEntryPoints.Size = new System.Drawing.Size(75, 37);
            this.btnEntryPoints.TabIndex = 27;
            this.btnEntryPoints.Text = "提取入口点";
            this.btnEntryPoints.UseVisualStyleBackColor = true;
            this.btnEntryPoints.Click += new System.EventHandler(this.btnEntryPoints_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnEntryPoints);
            this.groupBox1.Controls.Add(this.btnSubGraph);
            this.groupBox1.Location = new System.Drawing.Point(974, 19);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 81);
            this.groupBox1.TabIndex = 29;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "level2";
            // 
            // btnMetisData
            // 
            this.btnMetisData.Location = new System.Drawing.Point(12, 128);
            this.btnMetisData.Name = "btnMetisData";
            this.btnMetisData.Size = new System.Drawing.Size(75, 39);
            this.btnMetisData.TabIndex = 28;
            this.btnMetisData.Text = "METIS工具";
            this.btnMetisData.UseVisualStyleBackColor = true;
            this.btnMetisData.Click += new System.EventHandler(this.btnMetisData_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnUpdateVertexAdj);
            this.groupBox2.Controls.Add(this.btnPolygonize);
            this.groupBox2.Controls.Add(this.btnLineAdjaction);
            this.groupBox2.Location = new System.Drawing.Point(974, 316);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(200, 125);
            this.groupBox2.TabIndex = 30;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "level1";
            // 
            // btnUpdateVertexAdj
            // 
            this.btnUpdateVertexAdj.Location = new System.Drawing.Point(12, 82);
            this.btnUpdateVertexAdj.Name = "btnUpdateVertexAdj";
            this.btnUpdateVertexAdj.Size = new System.Drawing.Size(89, 38);
            this.btnUpdateVertexAdj.TabIndex = 2;
            this.btnUpdateVertexAdj.Text = "更新点-边表";
            this.btnUpdateVertexAdj.UseVisualStyleBackColor = true;
            this.btnUpdateVertexAdj.Click += new System.EventHandler(this.btnUpdateVertexAdj_Click);
            // 
            // btnPolygonize
            // 
            this.btnPolygonize.Location = new System.Drawing.Point(107, 19);
            this.btnPolygonize.Name = "btnPolygonize";
            this.btnPolygonize.Size = new System.Drawing.Size(81, 40);
            this.btnPolygonize.TabIndex = 1;
            this.btnPolygonize.Text = "生成多边形";
            this.btnPolygonize.UseVisualStyleBackColor = true;
            this.btnPolygonize.Click += new System.EventHandler(this.btnPolygonize_Click);
            // 
            // btnLineAdjaction
            // 
            this.btnLineAdjaction.Location = new System.Drawing.Point(107, 82);
            this.btnLineAdjaction.Name = "btnLineAdjaction";
            this.btnLineAdjaction.Size = new System.Drawing.Size(79, 40);
            this.btnLineAdjaction.TabIndex = 0;
            this.btnLineAdjaction.Text = "边-结点表";
            this.btnLineAdjaction.UseVisualStyleBackColor = true;
            this.btnLineAdjaction.Click += new System.EventHandler(this.btnLineAdjaction_Click);
            // 
            // cbxAlgorithm
            // 
            this.cbxAlgorithm.FormattingEnabled = true;
            this.cbxAlgorithm.Items.AddRange(new object[] {
            "Dijkstra",
            "Astar",
            "BdDijkstra",
            "BdAstar"});
            this.cbxAlgorithm.Location = new System.Drawing.Point(600, 210);
            this.cbxAlgorithm.Name = "cbxAlgorithm";
            this.cbxAlgorithm.Size = new System.Drawing.Size(121, 20);
            this.cbxAlgorithm.TabIndex = 31;
            // 
            // btnClassical
            // 
            this.btnClassical.Location = new System.Drawing.Point(769, 210);
            this.btnClassical.Name = "btnClassical";
            this.btnClassical.Size = new System.Drawing.Size(86, 36);
            this.btnClassical.TabIndex = 32;
            this.btnClassical.Text = "经典算法";
            this.btnClassical.UseVisualStyleBackColor = true;
            this.btnClassical.Click += new System.EventHandler(this.btnClassical_Click);
            // 
            // btnHierarchyAlgorthm
            // 
            this.btnHierarchyAlgorthm.Location = new System.Drawing.Point(769, 256);
            this.btnHierarchyAlgorthm.Name = "btnHierarchyAlgorthm";
            this.btnHierarchyAlgorthm.Size = new System.Drawing.Size(86, 36);
            this.btnHierarchyAlgorthm.TabIndex = 33;
            this.btnHierarchyAlgorthm.Text = "经典层次算法";
            this.btnHierarchyAlgorthm.UseVisualStyleBackColor = true;
            this.btnHierarchyAlgorthm.Click += new System.EventHandler(this.btnHierarchyAlgorthm_Click);
            // 
            // btnBDAlgorithm
            // 
            this.btnBDAlgorithm.Location = new System.Drawing.Point(769, 307);
            this.btnBDAlgorithm.Name = "btnBDAlgorithm";
            this.btnBDAlgorithm.Size = new System.Drawing.Size(86, 33);
            this.btnBDAlgorithm.TabIndex = 34;
            this.btnBDAlgorithm.Text = "双向层次算法";
            this.btnBDAlgorithm.UseVisualStyleBackColor = true;
            this.btnBDAlgorithm.Click += new System.EventHandler(this.btnBDAlgorithm_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnMETISTest);
            this.groupBox3.Controls.Add(this.cbxMetisAlgorithm);
            this.groupBox3.Controls.Add(this.txtPartNum);
            this.groupBox3.Controls.Add(this.label11);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.btnMetisData);
            this.groupBox3.Location = new System.Drawing.Point(974, 108);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(200, 202);
            this.groupBox3.TabIndex = 35;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "METIS";
            // 
            // cbxMetisAlgorithm
            // 
            this.cbxMetisAlgorithm.FormattingEnabled = true;
            this.cbxMetisAlgorithm.Items.AddRange(new object[] {
            "kway",
            "rb"});
            this.cbxMetisAlgorithm.Location = new System.Drawing.Point(78, 68);
            this.cbxMetisAlgorithm.Name = "cbxMetisAlgorithm";
            this.cbxMetisAlgorithm.Size = new System.Drawing.Size(108, 20);
            this.cbxMetisAlgorithm.TabIndex = 32;
            // 
            // txtPartNum
            // 
            this.txtPartNum.Location = new System.Drawing.Point(78, 26);
            this.txtPartNum.Name = "txtPartNum";
            this.txtPartNum.Size = new System.Drawing.Size(108, 21);
            this.txtPartNum.TabIndex = 31;
            this.txtPartNum.Text = "24";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(22, 73);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(41, 12);
            this.label11.TabIndex = 30;
            this.label11.Text = "算法：";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(10, 31);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(53, 12);
            this.label10.TabIndex = 30;
            this.label10.Text = "分区数：";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(529, 214);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(65, 12);
            this.label12.TabIndex = 13;
            this.label12.Text = "经典算法：";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(529, 316);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(65, 12);
            this.label13.TabIndex = 13;
            this.label13.Text = "双向算法：";
            // 
            // cbxBdAlgorithms
            // 
            this.cbxBdAlgorithms.FormattingEnabled = true;
            this.cbxBdAlgorithms.Items.AddRange(new object[] {
            "BdDijkstra",
            "BdAstar"});
            this.cbxBdAlgorithms.Location = new System.Drawing.Point(600, 313);
            this.cbxBdAlgorithms.Name = "cbxBdAlgorithms";
            this.cbxBdAlgorithms.Size = new System.Drawing.Size(121, 20);
            this.cbxBdAlgorithms.TabIndex = 31;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label14);
            this.groupBox4.Controls.Add(this.txtCostTime);
            this.groupBox4.Location = new System.Drawing.Point(520, 99);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(350, 317);
            this.groupBox4.TabIndex = 36;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "算法测试";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(32, 276);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(65, 12);
            this.label14.TabIndex = 13;
            this.label14.Text = "算法耗时：";
            // 
            // txtCostTime
            // 
            this.txtCostTime.Location = new System.Drawing.Point(125, 270);
            this.txtCostTime.Name = "txtCostTime";
            this.txtCostTime.Size = new System.Drawing.Size(127, 21);
            this.txtCostTime.TabIndex = 14;
            // 
            // btnMETISTest
            // 
            this.btnMETISTest.Location = new System.Drawing.Point(113, 128);
            this.btnMETISTest.Name = "btnMETISTest";
            this.btnMETISTest.Size = new System.Drawing.Size(75, 39);
            this.btnMETISTest.TabIndex = 33;
            this.btnMETISTest.Text = "测试";
            this.btnMETISTest.UseVisualStyleBackColor = true;
            this.btnMETISTest.Click += new System.EventHandler(this.btnMETISTest_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1288, 473);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.btnBDAlgorithm);
            this.Controls.Add(this.btnHierarchyAlgorthm);
            this.Controls.Add(this.btnClassical);
            this.Controls.Add(this.cbxBdAlgorithms);
            this.Controls.Add(this.cbxAlgorithm);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.txtPointCount);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.txtLayerName);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.btnAdjactList);
            this.Controls.Add(this.btnIntersect);
            this.Controls.Add(this.btnBreakLines);
            this.Controls.Add(this.btnGetSPath);
            this.Controls.Add(this.richTextBoxRange);
            this.Controls.Add(this.btnGetIDRange);
            this.Controls.Add(this.textBoxEndPoint);
            this.Controls.Add(this.textBoxStartPoint);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.btnAssignID);
            this.Controls.Add(this.btnBuildInterTable);
            this.Controls.Add(this.btnNewLines);
            this.Controls.Add(this.tbPort);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbDataBaseName);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tbPass);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tbUser);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbHost);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.GvwData);
            this.Controls.Add(this.btnConn);
            this.Controls.Add(this.groupBox4);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.GvwData)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnConn;
        private System.Windows.Forms.DataGridView GvwData;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbHost;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbPort;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbUser;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbPass;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbDataBaseName;
        private System.Windows.Forms.Button btnNewLines;
        private System.Windows.Forms.Button btnBuildInterTable;
        private System.Windows.Forms.Button btnAssignID;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxStartPoint;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBoxEndPoint;
        private System.Windows.Forms.Button btnGetIDRange;
        private System.Windows.Forms.RichTextBox richTextBoxRange;
        private System.Windows.Forms.Button btnGetSPath;
        private System.Windows.Forms.Button btnBreakLines;
        private System.Windows.Forms.Button btnIntersect;
        private System.Windows.Forms.Button btnAdjactList;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtLayerName;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtPointCount;
        private System.Windows.Forms.Button btnSubGraph;
        private System.Windows.Forms.Button btnEntryPoints;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnLineAdjaction;
        private System.Windows.Forms.Button btnPolygonize;
        private System.Windows.Forms.Button btnUpdateVertexAdj;
        private System.Windows.Forms.ComboBox cbxAlgorithm;
        private System.Windows.Forms.Button btnClassical;
        private System.Windows.Forms.Button btnMetisData;
        private System.Windows.Forms.Button btnHierarchyAlgorthm;
        private System.Windows.Forms.Button btnBDAlgorithm;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cbxMetisAlgorithm;
        private System.Windows.Forms.TextBox txtPartNum;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox cbxBdAlgorithms;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox txtCostTime;
        private System.Windows.Forms.Button btnMETISTest;
    }
}

