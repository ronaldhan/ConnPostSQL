using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Npgsql;
using System.IO;
using System.Collections;

namespace ConnPostSQL
{
    /// <summary>
    /// 20130228修改为适合带有kind字段的数据
    /// 以后写程序，尽量不在循环体内定义变量，将循环体内的变量在循环开始前定义好
    /// </summary>
    public partial class Form1 : Form
    {
        static DataTable dt;
        //设置计数器，用于标识新线段的gid
        //static int newgid = 0;
        DB postDB;
        //定义要操作的图层
        string layerName;
        //保存算法耗时
        string TimeCost;

        public Form1()
        {
            InitializeComponent();
            postDB = new DB();
        }

        private void btnConn_Click(object sender, EventArgs e)
        {
            try
            {
                postDB.DBServer = tbHost.Text;
                postDB.DBPort = tbPort.Text;
                postDB.DBUser = tbUser.Text;
                postDB.DBPwd = tbPass.Text;
                postDB.DBSID = tbDataBaseName.Text;
                postDB.Connection();
                layerName = txtLayerName.Text.Trim();
                if (layerName=="")
                {
                    MessageBox.Show("请输入图层名称");
                    return;
                }
                string sql = "SELECT a.gid,st_astext(a.the_geom) as geom_txt ,kind from " + layerName + " "+ "a order by gid,geom_txt asc";
                dt = postDB.DoQueryEx(sql);
                GvwData.DataSource = dt;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        public struct PostGISPoint
        {
            public double x;
            public double y;
        }

        private DataTable GetData(string layername)
        {
            DataTable tmdt;
            if (IsExist(layername))
            {
                string sqlstr = "select * from" + " " + layername;
                tmdt = postDB.DoQueryEx(sqlstr);
                return tmdt;
            }
            else
            {
                return null;
            }
        }

        private void btnNewLines_Click(object sender, EventArgs e)
        {
            try
            {
                //20130228
                //此处新道路只需要保存原有所属道路的kind即可
                string tablename = layerName + "_newlines";
                //删除line_gid字段，因为在其他查询中不适用此字段20130513
                string sqlstr = "create table" + " " + tablename + " (gid integer,kind integer)";
                CheckCreateTable(tablename, sqlstr);
                string geoColumn = "geom";
                string geoType = "MULTILINESTRING";
                //判断newlines表中是否有geom字段
                if (IsColumnExist(tablename, geoColumn))
                {
                    sqlstr = "alter table " + tablename + " " + "drop column geom cascade";
                    postDB.ExecNonQuery(sqlstr);
                    sqlstr = "select addgeometrycolumn('" + tablename + "', '" + geoColumn + "', 4326,'" + geoType + "',2)";
                    postDB.ExecNonQuery(sqlstr);
                } 
                else
                {
                    sqlstr = "select addgeometrycolumn('" + tablename + "', '" + geoColumn + "', 4326,'" + geoType + "',2)";
                    postDB.ExecNonQuery(sqlstr);
                }
                string ftablename = layerName + "_lines_intersect_unique";
                //使用交点构建线段
                //确定主循环体的范围
                PostGISPoint[] TmpPoints;
                List<int> vIndex;
                string[] xy, coor;
                string geomety, startpoint, endpoint, gid, kind;
                //标识新生成线段
                int newgid = 0;

                //主循环体
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    //将mls的所有坐标点存入自定义结构中，便于访问，也可以选择从数据库表中读取，速度比较慢
                    //获取地理坐标字符串,并对格式进行修改
                    geomety = dt.Rows[i]["geom_txt"].ToString();
                    coor = geomety.Split(',');
                    startpoint = coor[0].Substring(17);
                    int tmpLength = coor[coor.Length - 1].Length;
                    endpoint = coor[coor.Length - 1].Substring(0, tmpLength - 2);
                    coor[0] = startpoint;
                    coor[coor.Length - 1] = endpoint;
                    TmpPoints = new PostGISPoint[coor.Length];
                    //将坐标格式化存储
                    for (int k = 0; k < coor.Length; k++)
                    {
                        //xy数据使用空格分开
                        xy = coor[k].Split(' ');
                        TmpPoints[k].x = Convert.ToDouble(xy[0]);
                        TmpPoints[k].y = Convert.ToDouble(xy[1]);
                    }

                    gid = dt.Rows[i]["gid"].ToString();
                    kind = dt.Rows[i]["kind"].ToString();
                    //获取此mls的交点
                    //修改此处貌似也可以达到目标
                    //sqlstr = "select line_gid1, assvertex_index1,points from lines_intersect_unique1 where line_gid1=" + gid + "order by line_gid1,assvertex_index1,points asc";
                    sqlstr = "select line_gid1, svertex_index1, st_astext(points) as points_txt from" +" " + ftablename + " " + "where line_gid1=" + gid + "order by line_gid1,svertex_index1,points_txt asc";
                    DataTable pointTable = postDB.DoQueryEx(sqlstr);
                    int pointsCount = pointTable.Rows.Count;
                    //存放修改后的结点坐标
                    PostGISPoint[] resultPoints = TmpPoints;
                    //应该将此序列默认生成为值为0的序列，只修改相同点的值
                    vIndex = new List<int>();
                    for (int x = 0; x < resultPoints.Length;x++ )
                    {
                        vIndex.Add(0);
                    }

                    //统计插入点的个数，用于计算正确的vertexIndex
                    int count = 0;
                    int index = 0;
                    int nindex = 0;

                    //对交点做处理
                    for (int j = 0; j < pointsCount; j++)
                    {
                        int vertexIndex =Convert.ToInt32(pointTable.Rows[j]["svertex_index1"].ToString());
                        string point = pointTable.Rows[j]["points_txt"].ToString();
                        PostGISPoint interPoint = Con2PostGISPoint(point);

                        //将交点与mls结点进行比较，如果交点在结点序列中，对此结点标记1
                        //如果交点不在结点序列中，按照交点的vertexindex插入到序列中，并且对新结点标记1
                        index = FindPoint(resultPoints, interPoint);
                        if (index>=0)
                        {
                            vIndex[index] = 1;
                        } 
                        else
                        {
                            //交点不在结点序列，应该插入到vertexindex结点之后
                            //每插入一个点，vIndex也应该增加一个元素
                            //找到问题的原因，在于此处设定vindex的索引不正确
                            resultPoints=InsertPoint(resultPoints, vertexIndex + count, interPoint);
                            vIndex.Add(0);
                            nindex = FindIndex(resultPoints, interPoint);                            
                            vIndex[nindex] = 1;
                            count++;
                        }
                        
                    }
                    //需要对vIndex序列进行处理，将两个端点的值修改，并且将非1值填充为0
                    for (int m = 0; m < resultPoints.Length;m++ )
                    {
                        if (m==0 || m==resultPoints.Length-1)
                        {
                            vIndex[m] = 1;
                        } 
                        else
                        {
                            if (vIndex[m]>0)
                            {
                                continue;
                            }
                            else
                            {
                                vIndex[m] = 0;
                            }
                        }
                    }
                    //根据vIndex结果，对线段进行分割并存储
                    //存储值为1的索引号
                    List<int> nVIndex = new List<int>();
                    for (int n = 0; n < vIndex.Count; n++)
                    {
                        if (vIndex[n]==1)
                        {
                            nVIndex.Add(n);
                        }
                    }
                    for (int s = 1; s < nVIndex.Count;s++ )
                    {
                        int preIndex=nVIndex[s-1];
                        int curIndex=nVIndex[s];
                        string tmpGeom = "";
                        for (int q = preIndex; q <= curIndex;q++ )
                        {
                            //构造生成geometry的字符串，此字符串末位多一个冒号
                            tmpGeom = tmpGeom + resultPoints[q].x.ToString() + " " + resultPoints[q].y.ToString() + ",";
                        }
                        //去掉最后多余的冒号
                        tmpGeom = tmpGeom.Substring(0, tmpGeom.Length - 1);
                        tmpGeom = "MULTILINESTRING((" + tmpGeom + "))";
                        //将结果写入到表subroad1_newlines中
                        sqlstr = "INSERT INTO" +" " + tablename +" " +"(gid,geom,kind) values (" + newgid + "," + "st_geomfromtext('" + tmpGeom + "', 4326)" +"," + kind +")";
                        postDB.InsertRow(sqlstr);
                        newgid++;
                    }
                    
                }
                //监测运行结果
                DataTable tmpdt = GetData(tablename);
                GvwData.DataSource = tmpdt;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// 在点序列中查找给定点是否存在，存在返回索引值，不存在返回-1
        /// </summary>
        /// <param name="points"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private int FindPoint(PostGISPoint[] points, PostGISPoint point)
        {
            PostGISPoint tp = new PostGISPoint();
            int k = -1;
            for (int i = 0; i < points.Length;i++)
            {
                tp = points[i];
                if (IsSamePoint(tp,point,0.00000001))
                {
                    return i;
                } 
            }
            return k;
        }

        /// <summary>
        /// 查找特定点在点数组中的索引值
        /// </summary>
        /// <param name="points"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private int FindIndex(PostGISPoint[] points, PostGISPoint point)
        {
            PostGISPoint tp = new PostGISPoint();
            for (int i = 0; i < points.Length; i++)
            {
                tp = points[i];
                if (IsSamePoint(tp, point, 0.00000001))
                {
                    return i;
                }
            }
            return 0;
        }

        /// <summary>
        /// 在指定索引处插入点
        /// </summary>
        /// <param name="points"></param>
        /// <param name="index"></param>
        /// <param name="point"></param>
        private PostGISPoint[] InsertPoint(PostGISPoint[] points, int index, PostGISPoint point)
        {
            PostGISPoint[] tp = new PostGISPoint[points.Length+1];
            for (int i = 0; i < tp.Length;i++ )
            {
                if (i==index+1)
                {
                    tp[i] = point;
                } 
                else if (i<=index)
                {
                    tp[i] = points[i];
                }
                else
                {
                    tp[i] = points[i - 1];
                }
            }
            //points = tp;
            return tp;
        }

        /// <summary>
        /// 从几何字段的文本中提取出坐标，并用自定义结构存储
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private PostGISPoint Con2PostGISPoint(string point)
        {
            int index=point.LastIndexOf(')');
            string tmpPoint = point.Substring(6, index - 6);
            string[] xy = tmpPoint.Split(' ');
            PostGISPoint tmpGISPoint = new PostGISPoint();
            tmpGISPoint.x = Convert.ToDouble(xy[0]);
            tmpGISPoint.y = Convert.ToDouble(xy[1]);
            return tmpGISPoint;
        }

        /// <summary>
        /// 判断点是否为一条mls的起点或终点
        /// </summary>
        /// <param name="tmpPoints"></param>
        /// <returns></returns>
        private bool IsSE(PostGISPoint[] tmpPoints, PostGISPoint point)
        {
            PostGISPoint tp = new PostGISPoint();
            bool b1,b2;
            tp = tmpPoints[0];
            //经纬度表示下，小数点后5位相当于10m的容差限
            b1 = IsSamePoint(tp, point, 0.00000001);
            tp = tmpPoints[tmpPoints.Length-1];
            b2 = IsSamePoint(tp, point, 0.00000001);
            if (b1 && b2)
            {
                return true;
            } 
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断点是否落在指定索引线段之间，不为起点或终点，返回值为1，其他返回0
        /// </summary>
        /// <param name="tmpPoints"></param>
        /// <param name="index"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        private int IsBetweenSE(PostGISPoint[] tmpPoints, int index, PostGISPoint point)
        {
            PostGISPoint tp = new PostGISPoint();
            tp = tmpPoints[index];
            bool b1,b2;
            //经纬度表示下，小数点后5位相当于10m的容差限
            b1 = IsSamePoint(tp, point, 0.00000001);
            tp = tmpPoints[index+1];
            b2 = IsSamePoint(tp, point, 0.00000001);
            if (b1 && b2)
            {
                return (int)1;
            } 
            else
            {
                return (int)0;
            }

        }

        /// <summary>
        /// 判断两个点在容差范围内是不是同一个点，此处使用最简单的比较方式，另外可以使用缓冲区判断
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        private bool IsSamePoint(PostGISPoint point1, PostGISPoint point2, double tolerance)
        {
            double dx, dy;
            dx = Math.Abs(point1.x - point2.x);
            dy = Math.Abs(point1.y - point2.y);
            if (dx<tolerance && dy<tolerance)
            {
                return true;
            } 
            else
            {
                return false;
            }
        }

        private bool IsExist(string layername)
        {
            string sqlstr = "select count(*) from pg_class where relname = '" + layername + "'";
            DataTable tmpdt = postDB.DoQueryEx(sqlstr);
            string tmpCount = tmpdt.Rows[0][0].ToString();
            int num = Convert.ToInt32(tmpCount);
            if (num>0)
            {
                return true;
            } 
            else
            {
                return false;
            }
        }

        private bool IsColumnExist(string tablename,string columnname)
        {
            string sqlstr = "SELECT a.attname,pg_catalog.format_type(a.atttypid, a.atttypmod) AS data_type" + " "
                                + "FROM    pg_catalog.pg_attribute a,(SELECT  c.oid FROM    pg_catalog.pg_class c " + " "
                                + "LEFT JOIN pg_catalog.pg_namespace n  ON n.oid = c.relnamespace  WHERE (c.relname) =lower(' " + tablename + "')" + " "
                                + "AND (n.nspname) = lower('public')) b WHERE a.attrelid = b.oid AND a.attnum > 0 AND NOT a.attisdropped ORDER BY a.attnum";
            DataTable tmpdt = postDB.DoQueryEx(sqlstr);
            for (int i = 0; i <= tmpdt.Rows.Count - 1;i++ )
            {
                string column=tmpdt.Rows[i][0].ToString();
                if (column.ToLower()==columnname.ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsIndexExist(string tablename, string indexname)
        {
            string sqlstr = "SELECT * from pg_indexes where tablename= '" + tablename + "'";
            DataTable tmpdt = postDB.DoQueryEx(sqlstr);
            for (int i = 0; i <= tmpdt.Rows.Count - 1; i++)
            {
                //第三列是index name
                string tmpnamestr = tmpdt.Rows[i][2].ToString();
                if (tmpnamestr.ToLower() == indexname.ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        private void DeleteTable(string layername)
        {
            int affect = postDB.DeleteTable(layername);
        }

        private void btnBuildInterTable_Click(object sender, EventArgs e)
        {
            //20130228
            string tablename = layerName + "_lines_intersect_unique";
            string sqlstr = "create table" + " " + tablename + " (line_gid1 integer,svertex_index1 integer,points geometry,kind1 integer,kind2 integer)";
            CheckCreateTable(tablename, sqlstr);
            string ftablename = layerName + "_lines_intersect";
            //统计每条mls的交点，并写入到新的表中
            for (int i = 0; i < dt.Rows.Count;i++ )
            {
                string gid = dt.Rows[i][0].ToString();
                //string sqlstr = "select distinct on (points) points, line_gid1, assvertex_index1 from lines_intersect where line_gid1=" + gid + "order by points,line_gid1,assvertex_index1 asc";
                sqlstr = "select distinct on (points) points, line_gid1, svertex_index1 ,kind1, kind2 from" +" " +  ftablename +" "+ "where line_gid1=" + gid + "order by points,line_gid1,svertex_index1, kind1,kind2 asc";
                DataTable pointTable = postDB.DoQueryEx(sqlstr);
                string vIndex, tmpGeom;
                int kind1, kind2;
                for (int j = 0; j < pointTable.Rows.Count;j++ )
                {
                    vIndex = pointTable.Rows[j][2].ToString();
                    tmpGeom = pointTable.Rows[j][0].ToString();
                    kind1 = Convert.ToInt32(pointTable.Rows[j][3].ToString());
                    kind2 = Convert.ToInt32(pointTable.Rows[j][4].ToString());
                    //sqlstr = "INSERT INTO lines_intersect_unique(points,line_gid1,svertex_index1) values (" + "st_geomfromtext('" + tmpGeom + "', 4326)" +","+  gid + "," + vIndex + ")";
                    sqlstr = "INSERT INTO" +" " +  tablename +" " + "(points,line_gid1,svertex_index1,kind1,kind2) values (" + "st_geomfromtext('" + tmpGeom + "', 4326)" + "," + gid + "," + vIndex + "," + kind1 + "," +kind2 + ")";
                    postDB.InsertRow(sqlstr);
                }
            }
            //监测运行结果
            DataTable tmpdt = GetData(tablename);
            GvwData.DataSource = tmpdt;
        }

        private void btnAssignID_Click(object sender, EventArgs e)
        {
            //20130305
            string tablename = layerName+"_newlines";
            double tolerance= 0.0000000089;
            string geomColumn="geom";
            string gidColumn="gid";
            string sqlstr;
            if (IsColumnExist(tablename,"source"))
            {
                sqlstr = "alter table " + tablename + " " + "drop column source cascade";
                postDB.ExecNonQuery(sqlstr);          
                sqlstr="alter table " + tablename + " " + "add column source integer";
                postDB.ExecNonQuery(sqlstr);
            } 
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column source integer";
                postDB.ExecNonQuery(sqlstr);  
            }
            if (IsColumnExist(tablename, "target"))
            {
                sqlstr = "alter table " + tablename + " " + "drop column target cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column target integer";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column target integer";
                postDB.ExecNonQuery(sqlstr);  
            }
            sqlstr = "select assign_vertex_id('" + tablename + "', " + tolerance + ", '" + geomColumn + "', '" + gidColumn + "')";
            postDB.ExecNonQuery(sqlstr);
            //分配id过程中产生了vertices_tmp表，需要将此表的数据备份存储用于生成邻接表
            tablename = layerName + "_vertices";
            sqlstr = "create table " + tablename + " as select * from vertices_tmp";
            CheckCreateTable(tablename, sqlstr);

            //结点更新完毕后需要将及时更新到level1_newlines中
            tablename=layerName+"_newlines";
            string distTableName="level1_newlines";
            sqlstr = "create table " + distTableName + " as select * from " + tablename + " " + "where kind=1";
            postDB.CheckCreateTable(distTableName, sqlstr);
            DataTable tmpdt = GetData(tablename);
            GvwData.DataSource = tmpdt;
        }

        private void btnGetIDRange_Click(object sender, EventArgs e)
        {
            string ftablename = layerName + "_newlines";
            string sqlstr = "select max(source),min(source),max(target),min(target) from  " + ftablename + "";
            DataTable dt = postDB.DoQueryEx(sqlstr);
            string smax = dt.Rows[0][0].ToString();
            string smin = dt.Rows[0][1].ToString();
            string tmax = dt.Rows[0][2].ToString();
            string tmin = dt.Rows[0][3].ToString();
            this.richTextBoxRange.Text = "起点的索引范围为：" + "\n" + smin + "--" + smax + "\n" + "终点的索引范围为：" + "\n" + tmin + "--" + tmax;
        }

        /// <summary>
        /// shortest_path函数使用的是Dijkstra算法
        /// 还可以再改进代码，选中不同的算法时显示不同的参数设置界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGetSPath_Click(object sender, EventArgs e)
        {
            string ftablename;
            string tablename;
            string geoColumn;
            string algorithm;
            
            ftablename = layerName + "_newlines";
            geoColumn = "geom";
            tablename = "";
            string start = this.textBoxStartPoint.Text.Trim();
            string end = this.textBoxEndPoint.Text.Trim();
            algorithm = cbxAlgorithm.SelectedItem.ToString().Trim();
            SetRoadLength(ftablename, geoColumn, "2345");
            ShortestPath spath = new ShortestPath(postDB);
            Timing timeRecorder = new Timing();
            switch (algorithm)
            {
                case "Dijkstra":
                    timeRecorder.Start();
                    spath.ComputePath(algorithm,ftablename,start,end);
                    tablename = layerName + "_dijkstra";
                    timeRecorder.Stop();
                    TimeCost = timeRecorder.Result();
                    break;
                case "Astar":
                    SetCoordinate(ftablename, geoColumn);
                    timeRecorder.Start();
                    spath.ComputePath(algorithm, ftablename, start, end);
                    tablename = layerName + "_astar";
                    timeRecorder.Stop();
                    TimeCost = timeRecorder.Result();
                    break;
                case "BdDijkstra":
                    timeRecorder.Start();
                    spath.ComputePath(algorithm, ftablename, start, end);
                    tablename = layerName + "_bddijkstra";
                    timeRecorder.Stop();
                    TimeCost = timeRecorder.Result();
                    break;
                case "BdAstar":
                    SetCoordinate(ftablename, geoColumn);
                    timeRecorder.Start();
                    spath.ComputePath(algorithm,ftablename,start,end);
                    tablename = layerName + "_bdastar";
                    timeRecorder.Stop();
                    TimeCost = timeRecorder.Result();
                    break;
                default:
                    break;
                /*
                case "ShootingStar":
                    start = this.txtStartEdge.Text.ToString();
                    end = this.txtEndEdge.Text.ToString();
                    SetCoordinate(ftablename, geoColumn);
                    SetCostColumn(ftablename);
                    spath.ShootingStar(ftablename, start, end);
                    tablename = layerName + "_shootingstar";
                    UpdateShortestResult(ftablename, tablename);
                    break;
                 * */             
            }
            this.txtCostTime.Text = TimeCost.ToString();
            GvwData.DataSource = GetData(tablename);
        }


        private void btnBreakLines_Click(object sender, EventArgs e)
        {
            try
            {
                //20130228
                string sqlstr = "SELECT a.gid,st_astext(a.the_geom) as geom_txt, a.kind from" + " " + layerName + " " + "a order by gid,geom_txt,kind asc";
                dt = postDB.DoQueryEx(sqlstr);                

                //用于存储图层点的个数
                int sumPointCount = 0;
                //北京市三级道路最长的路段有948个点组成
                PostGISPoint[] TmpPoints;
                //保存结果的表名
                string tablename = layerName + "_lines";
                //检测表是否存在，存在则删除，不存在则创建
                //20130228
                sqlstr = "create table" + " " + tablename + " (line_gid integer,svertex_index integer,geom geometry,kind integer)";
                CheckCreateTable(tablename, sqlstr);
                //主循环体，将所有线段打断
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    //获取地理坐标字符串,并对格式进行修改
                    string geomety = dt.Rows[i][1].ToString();
                    string[] coor = geomety.Split(',');
                    string startpoint = coor[0].Substring(17);
                    int tmpLength = coor[coor.Length - 1].Length;
                    string endpoint = coor[coor.Length - 1].Substring(0, tmpLength - 2);
                    coor[0] = startpoint;
                    coor[coor.Length - 1] = endpoint;
                    TmpPoints = new PostGISPoint[coor.Length];
                    sumPointCount += coor.Length;
                    
                    //将坐标格式化存储
                    for (int j = 0; j < coor.Length; j++)
                    {
                        //xy数据使用空格分开
                        string[] xy = coor[j].Split(' ');
                        char[] trim=new char[2];
                        trim[0] = ')';
                        trim[1] = '(';
                        //针对中间带有括号的存储，需要将括号过滤掉
                        xy[0] = xy[0].Trim(trim);
                        xy[1] = xy[1].Trim(trim);
                        TmpPoints[j].x = Convert.ToDouble(xy[0]);
                        TmpPoints[j].y = Convert.ToDouble(xy[1]);
                    }
                    //将数据写入到表中
                    int gid, svertex_index, kind;
                    string geom;
                    for (int k = 0; k < TmpPoints.Length - 1; k++)
                    {
                        //将数据写入到表中，存放格式为gid，svertex_index,geom
                        //20130228
                        gid = Convert.ToInt32(dt.Rows[i]["gid"].ToString());
                        svertex_index = k;
                        geom = "LINESTRING(" + TmpPoints[k].x.ToString() + " " + TmpPoints[k].y.ToString() + "," + TmpPoints[k + 1].x.ToString() + " " + TmpPoints[k + 1].y.ToString() + ")";
                        kind = Convert.ToInt32(dt.Rows[i]["kind"].ToString());
                        sqlstr = "INSERT INTO" +" "+ tablename + "(line_gid,svertex_index,geom,kind) values (" + gid + "," + k + "," + "st_geomfromtext('" + geom + "', 4326)" + "," + kind + ")";
                        postDB.InsertRow(sqlstr);
                    }
                }
                txtPointCount.Text = sumPointCount.ToString();
                //监测运行结果
                DataTable tmpdt = GetData(tablename);
                GvwData.DataSource = tmpdt;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void btnIntersect_Click(object sender, EventArgs e)
        {
            try
            {
                string tablename = layerName + "_lines_intersect";
                string ftablename=layerName +"_lines";
                //首先检测ftablename表上是否创建索引
                string indexGeometry = ftablename + "_index_geom";
                string indexId = ftablename + "_index_id";
                string geomColumn = "geom";
                string gidColumn = "line_gid";
                string sqlstr;
                if (IsIndexExist(ftablename, indexGeometry))
                {
                    sqlstr = "drop index " + " " + indexGeometry;
                    postDB.ExecNonQuery(sqlstr);
                    sqlstr = "create index" +" " + indexGeometry + " " + "on"+ " " + ftablename + " "+ "using gist(" + geomColumn + ")";
                    postDB.ExecNonQuery(sqlstr);
                }
                else
                {
                    sqlstr = "create index" + " " + indexGeometry + " " + "on" + " " + ftablename + " " + "using gist(" + geomColumn + ")";
                    postDB.ExecNonQuery(sqlstr);
                }
                if (IsIndexExist(ftablename, indexId))
                {
                    sqlstr = "drop index " + " " + indexId;
                    postDB.ExecNonQuery(sqlstr);
                    sqlstr = "create index" + " " + indexId + " " + "on" + " " + ftablename + " " + "using btree(" + gidColumn + ")";
                    postDB.ExecNonQuery(sqlstr);
                }
                else
                {
                    sqlstr = "create index" + " " + indexId + " " + "on" + " " + ftablename + " " + "using btree(" + gidColumn + ")";
                    postDB.ExecNonQuery(sqlstr);
                }
                //20130228
                sqlstr = "create table" + " " + tablename + " " + "as select a.line_gid as line_gid1,a.svertex_index as svertex_index1,"
                                    + "b.line_gid as line_gid2,b.svertex_index as svertex_index2,st_astext(st_intersection(a.geom,b.geom)) as points,"
                                    +"a.kind as kind1,b.kind as kind2,st_intersection(a.geom,b.geom) as geom" + " "
                                    + "from " + " " + ftablename +" " + "a," + ftablename + " " + "b where a.geom && b.geom" + " "
                                    + "and st_isempty(st_intersection(a.geom,b.geom))=false and a.line_gid<>b.line_gid";
                CheckCreateTable(tablename, sqlstr);
                //监测运行结果
                DataTable tmpdt = GetData(tablename);
                GvwData.DataSource = tmpdt;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void CheckCreateTable(string tablename, string sqlstr)
        {
            if (IsExist(tablename))
            {
                //先删除原有表，再创建新表
                postDB.DeleteTable(tablename);
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                postDB.ExecNonQuery(sqlstr);
            }
        }

        private void btnAdjactList_Click(object sender, EventArgs e)
        {
            //构建结点的邻接关系表
            //操作的对象为vertices_tmp表，以后此过程可以放到分配id过程之后进行
            //首先检测要创建的表是否存在
            //表字段分别为结点编号，邻接结点数目，邻接结点编号，邻接边编号，结点坐标
            //为操作的表的source和target字段添加b-tree索引
            string tablename = layerName + "_newlines";
            string IndexSource = layerName + "_index_source";
            string IndexTarget = layerName + "_index_target";
            string IndexColumnSource = "source";
            string IndexColumnTarget = "target";
            string sqlstr;
            if (IsIndexExist(tablename, IndexSource))
            {
                sqlstr = "drop index " + " " + IndexSource;
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "create index" + " " + IndexSource + " " + "on" + " " + tablename + " " + "using btree(" + IndexColumnSource + ")";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "create index" + " " + IndexSource + " " + "on" + " " + tablename + " " + "using btree(" + IndexColumnSource + ")";
                postDB.ExecNonQuery(sqlstr);
            }
            if (IsIndexExist(tablename, IndexTarget))
            {
                sqlstr = "drop index " + " " + IndexTarget;
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "create index" + " " + IndexTarget + " " + "on" + " " + tablename + " " + "using btree(" + IndexColumnTarget + ")";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "create index" + " " + IndexTarget + " " + "on" + " " + tablename + " " + "using btree(" + IndexColumnTarget + ")";
                postDB.ExecNonQuery(sqlstr);
            }

            //20130301
            //修改表结构，添加kind字段
            tablename = layerName + "_vertex_adjaction";
            sqlstr = "create table" + " " + tablename + " (vertex_id integer,vertex_adj_count integer,vertex_adj_id text,line_adj_id text,geom geometry,kind integer)";
            CheckCreateTable(tablename, sqlstr);
            sqlstr = "select id, st_astext(the_geom) as geom from " + layerName + "_vertices";
            DataTable tmpdt=postDB.DoQueryEx(sqlstr);

            //20130301
            //邻接表中已经统计结点关联的边，在此通过对比确定结点的等级
            string vertex_id, geom, vertex_adj_id, line_adj_id,vertex_kind;
            int vertex_adj_count;
            //DataTable dt_source, dt_target,dt_kind;
            //记录每个结点的查询结果
            DataTable dt_record;
            List<int> kind;
            List<string> vertex_adj;
            List<string> line_adj;
            //20130323
            string tmpSource, tmpTarget;

            //int tmpKind;
            for (int i = 0; i < tmpdt.Rows.Count;i++ )
            {
                vertex_id = tmpdt.Rows[i]["id"].ToString();
                geom = tmpdt.Rows[i]["geom"].ToString();
                
                vertex_adj_id = null;
                line_adj_id = null;
                kind = new List<int>();
                //20130322
                vertex_adj = new List<string>();
                line_adj = new List<string>();
#region 原有代码
                //20130305
                //sqlstr = "select gid,target from " + " " + layerName + "_newlines" + " where source=" + vertex_id;
                //dt_source = postDB.DoQueryEx(sqlstr);
                //sqlstr = "select gid,source from " + " " + layerName + "_newlines" + " where target=" + vertex_id;
                //dt_target = postDB.DoQueryEx(sqlstr);
                //vertex_adj_count = dt_source.Rows.Count + dt_target.Rows.Count;
                //if (dt_source.Rows.Count > 0)
                //{
                //    for (int j = 0; j < dt_source.Rows.Count; j++)
                //    {
                //        if (! vertex_adj.Contains(dt_source.Rows[j][1].ToString()))
                //        {
                //            vertex_adj.Add(dt_source.Rows[j][1].ToString());
                //        }
                //        if (! line_adj.Contains(dt_source.Rows[j][0].ToString()))
                //        {
                //            line_adj.Add(dt_source.Rows[j][0].ToString());
                //        }
                //        //将关联边的kind添加到list中
                //        line_id = dt_source.Rows[j][0].ToString();
                //        sqlstr = "select kind from" + " " + layerName + "_newlines" + " " + "where gid=" + line_id;
                //        dt_kind = postDB.DoQueryEx(sqlstr);
                //        tmpKind = Convert.ToInt32(dt_kind.Rows[0][0].ToString());
                //        kind.Add(tmpKind);
                //    }
                //}
                //if (dt_target.Rows.Count > 0)
                //{
                //    for (int l = 0; l < dt_target.Rows.Count; l++)
                //    {
                //        if (!vertex_adj.Contains(dt_target.Rows[l][1].ToString()))
                //        {
                //            vertex_adj.Add(dt_target.Rows[l][1].ToString());
                //        }
                //        if (!line_adj.Contains(dt_target.Rows[l][0].ToString()))
                //        {
                //            line_adj.Add(dt_target.Rows[l][0].ToString());
                //        }
                //        //将关联边的kind添加到list中
                //        line_id = dt_target.Rows[l][0].ToString();
                //        sqlstr = "select kind from" + " " + layerName + "_newlines" + " " + "where gid=" + line_id;
                //        dt_kind = postDB.DoQueryEx(sqlstr);
                //        tmpKind = Convert.ToInt32(dt_kind.Rows[0][0].ToString());
                //        kind.Add(tmpKind);
                //    }
                //}
                //for (int m = 0; m < vertex_adj.Count;m++ )
                //{
                //    if (m < vertex_adj.Count-1)
                //    {
                //        vertex_adj_id += vertex_adj[m] + "_";
                //    } 
                //    else
                //    {
                //        vertex_adj_id += vertex_adj[m];
                //    }
                //}
                //for (int n = 0; n < line_adj.Count; n++)
                //{
                //    if (n < line_adj.Count - 1)
                //    {
                //        line_adj_id += line_adj[n] + "_";
                //    }
                //    else
                //    {
                //        line_adj_id += line_adj[n];
                //    }
                //}
#endregion

#region 新代码
                //20130323
                sqlstr = "select gid,kind,source,target from " + " " + layerName + "_newlines" + " where source=" + vertex_id + " or target=" + vertex_id;
                dt_record = postDB.DoQueryEx(sqlstr);
                vertex_adj_count = dt_record.Rows.Count;
                for (int j = 0; j < dt_record.Rows.Count;j++ )
                {
                    //邻接的边
                    line_adj.Add(dt_record.Rows[j]["gid"].ToString());
                    //邻接边的等级
                    kind.Add(Convert.ToInt32(dt_record.Rows[j]["kind"].ToString()));
                    //邻接的结点，需要根据source和target的不同进行判断
                    tmpSource = dt_record.Rows[j]["source"].ToString();
                    tmpTarget = dt_record.Rows[j]["target"].ToString();
                    if (tmpSource==vertex_id)
                    {
                        // 此时当前边的target是结点的邻结点
                        vertex_adj.Add(tmpTarget);
                    }
                    if (tmpTarget==vertex_id)
                    {
                        //当前边的target是此结点的邻接点
                        vertex_adj.Add(tmpSource);
                    }
                }
                //构造填充到表中的字符串
                for (int m = 0; m < dt_record.Rows.Count;m++ )
                {
                    vertex_adj_id += vertex_adj[m] + "_";
                    line_adj_id += line_adj[m] + "_";
                }
#endregion
                //20130301
                //根据结点关联的边的kind，确定结点的kind，取最高等级道路的kind作为结点的kind
                kind.Sort();
                vertex_kind = kind[0].ToString();
                //需要对vertex_adj_id和line_adj_id进行trim处理，去掉最后一个符号
                //vertex_adj_id = vertex_adj_id.Substring(0, vertex_adj_id.Length - 1);
                //line_adj_id = line_adj_id.Substring(0, line_adj_id.Length - 1);
                vertex_adj_id=vertex_adj_id.TrimEnd('_');
                line_adj_id=line_adj_id.TrimEnd('_');
                sqlstr = "INSERT INTO" + " " + tablename + " " + "(vertex_id,vertex_adj_count,vertex_adj_id,line_adj_id,geom,kind) values (" + vertex_id + "," 
                             + vertex_adj_count + ",'" + vertex_adj_id + "','" + line_adj_id + "'," + "st_geomfromtext('" + geom + "', 4326)" + "," + vertex_kind + ")";
                postDB.ExecNonQuery(sqlstr);
            }
            //监测运行结果
            DataTable dt_result = GetData(tablename);
            GvwData.DataSource = dt_result;
        }

        private void btnSubGraph_Click(object sender, EventArgs e)
        {
            //操作的表是邻接表，首先要添加两个字段，mark字段和subgraphnum字段
            //首先检查两个字段是否存在
            //此处算法操作数据为level2，使用kind=1的道路划分kind=2的道路
            string sqlstr, columnName, tablename;
            DataTable tmpdt;
            //List<int> vertex;
            Stack<int> qVertex;
            int stdKind;
            int subGraphNum = 1;
            int vertexCount;

            columnName = "subgraphnum";
            tablename = layerName + "_vertex_adjaction";
            qVertex = new Stack<int>();
            stdKind = 1;

            if (IsColumnExist(tablename, columnName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column" + " " + columnName + " " + "cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column" + " " + columnName + " " + "integer";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + " " + "=0";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column" + " " + columnName + " " + "integer";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + " " + "=0";
                postDB.ExecNonQuery(sqlstr);
            }
            columnName = "mark";
            if (IsColumnExist(tablename, columnName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column" + " " + columnName + " " + "cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column" + " " + columnName + " " + "boolean";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + " " + "=false";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column" + " " + columnName + " " + "boolean";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + " " + "=false";
                postDB.ExecNonQuery(sqlstr);
            }
            //将查询的结果按照vertex_id升序排列，便于根据vertex_id对行中数据进行更新
            //此时vertex_id和行记录的索引号相差1
            sqlstr = "select count(*) from" + " " + tablename;
            tmpdt = postDB.DoQueryEx(sqlstr);
            vertexCount = int.Parse(tmpdt.Rows[0]["count"].ToString());

            int vertex_id, kind, adj_count,tmpvertex,tmpKind;
            string[] adj_vertex;
            string adjvertex, isMark;
            DataTable tmpKindTable;

#region 原有代码
            //for (int i = 0; i < tmpdt.Rows.Count; i++)
            //{
            //    vertex_id = Convert.ToInt32(tmpdt.Rows[i]["vertex_id"].ToString());
            //    subNum = tmpdt.Rows[i]["subgraphnum"].ToString();
            //    kind = Convert.ToInt32(tmpdt.Rows[i]["kind"].ToString());
            //    isMark = tmpdt.Rows[i]["mark"].ToString();

            //    if (subNum == "0" && kind > stdKind && isMark.ToLower()=="false")
            //    {
            //        //为当前结点赋予区域号，并做标记
            //        //获取要删除的结点的id
            //        sqlstr = "update " + tablename + " " + "set subgraphnum=" + subGraphNum.ToString() + ", mark=true" + " " + "where vertex_id=" + vertex_id.ToString();
            //        postDB.ExecNonQuery(sqlstr);
            //        qVertex.Push(vertex_id);
            //        while (qVertex.Count > 0)
            //        {
            //            //需要根据id，在datatable中查找出对应的一条记录
            //            tmpid = qVertex.Pop();
            //            sqlstr = "select vertex_adj_count,vertex_adj_id from " + tablename + " where vertex_id=" + tmpid;
            //            adjTable = postDB.DoQueryEx(sqlstr);
            //            adjvertex = adjTable.Rows[0]["vertex_adj_id"].ToString();
            //            adj_count = Convert.ToInt32(adjTable.Rows[0]["vertex_adj_count"].ToString());
            //            adj_vertex = adjvertex.Split('_');

            //            for (int j = 0; j < adj_count; j++)
            //            {

            //                //将当前结点邻接点中，所连接的边的kind>stdkind的结点加入到队列中
            //                tmpvertex = Convert.ToInt32(adj_vertex[j]);
            //                sqlstr = "select kind,mark from " + tablename + " where vertex_id=" + tmpvertex;
            //                tmpKindTable = postDB.DoQueryEx(sqlstr);
            //                tmpKind = Convert.ToInt32(tmpKindTable.Rows[0]["kind"].ToString());
            //                isMark = tmpKindTable.Rows[0]["mark"].ToString();
            //                if (tmpKind > stdKind && isMark.ToLower() == "false")
            //                {
            //                    qVertex.Push(tmpvertex);
            //                }
            //            }
            //        }
            //        subGraphNum += 1;
            //    }
            //    else if (kind == stdKind && subNum == "0")
            //    {
            //        tmpdt.Rows[i]["mark"] = "true";
            //        tmpdt.Rows[i]["subgraphnum"] = 0;
            //    }
            //    else
            //    {
            //        tmpdt.Rows[i]["mark"] = "true";
            //        continue;
            //    }
            //}
#endregion
            
#region 新代码
            //主循环
            for (int i = 0; i < vertexCount; i++)
            {
                sqlstr = "select vertex_id,vertex_adj_count,vertex_adj_id,line_adj_id,kind,subgraphnum,mark from" + " " + tablename + " "
                     + "order by vertex_id,vertex_adj_count,vertex_adj_id,line_adj_id,kind,subgraphnum,mark asc";
                tmpdt = postDB.DoQueryEx(sqlstr);
                vertex_id = Convert.ToInt32(tmpdt.Rows[i]["vertex_id"].ToString());
                kind = Convert.ToInt32(tmpdt.Rows[i]["kind"].ToString());
                isMark = tmpdt.Rows[i]["mark"].ToString();
                if (isMark.ToLower()=="false" && kind > stdKind)
                {
                    qVertex.Push(vertex_id);
                    while (qVertex.Count > 0)
                    {
                        tmpvertex = qVertex.Pop();
                        sqlstr = "update " + tablename + " " + "set subgraphnum=" + subGraphNum.ToString() + ", mark=" +"true"+ " " + "where vertex_id=" + tmpvertex.ToString();
                        postDB.ExecNonQuery(sqlstr);
                        adjvertex = tmpdt.Rows[tmpvertex - 1]["vertex_adj_id"].ToString();
                        adj_count = Convert.ToInt32(tmpdt.Rows[tmpvertex - 1]["vertex_adj_count"].ToString());
                        adj_vertex = adjvertex.Split('_');
                        string adjvtx;
                        for (int j = 0; j < adj_vertex.Length; j++)
                        {
                            adjvtx = adj_vertex[j];
                            sqlstr = "select kind,mark from " + tablename + " where vertex_id=" + adjvtx;
                            tmpKindTable = postDB.DoQueryEx(sqlstr);
                            tmpKind = Convert.ToInt32(tmpKindTable.Rows[0]["kind"].ToString());
                            isMark = tmpKindTable.Rows[0]["mark"].ToString();
                            if (tmpKind > stdKind && isMark.ToLower() == "false")
                            {
                                qVertex.Push(Convert.ToInt32(adjvtx));
                            }
                        }
                    }
                    subGraphNum += 1;
                }
                else if(kind==stdKind)
                {
                    sqlstr = "update " + tablename + " " + "set subgraphnum=0" + ", mark=true" + " " + "where vertex_id=" + vertex_id.ToString();
                    postDB.ExecNonQuery(sqlstr);
                }
                else
                {
                    continue;
                }
            }
#endregion
            //分区结束后将分区号传递到多边形中
            PolygonNum();
            //将分区结果传递到newlines表中
            NewlinesNum();
            //测试结果
            tmpdt = GetData(tablename);
            this.GvwData.DataSource = tmpdt;

        }

        private void btnEntryPoints_Click(object sender, EventArgs e)
        {
            //操作的图层是邻接表，结果产生一个入口点表，需要记录点的控制区域
            string tablename,sqlstr,stdkind,adjlines,tmpline;
            DataTable tmpdt,tmplinedt;
            string[] adj_lines;
            int tmpkind,tmpcount;
            string point_id,txtgeom,ctrlsubgraph;
            
            //检测结果表是否存在
            tablename = layerName + "_entrypoint";
            sqlstr = "create table" + " " + tablename + " (point_id integer,kind integer,controlsubgraph text,geom geometry)";
            CheckCreateTable(tablename, sqlstr);
            stdkind="1";
            sqlstr = "select vertex_id,vertex_adj_id,line_adj_id,st_astext(geom) as geom,kind from " + layerName + "_vertex_adjaction" + " " + "where kind=" + stdkind + " and vertex_adj_count>=3";
            tmpdt=postDB.DoQueryEx(sqlstr);

            //对选出的结点进行筛选
            for (int i = 0; i < tmpdt.Rows.Count;i++ )
            {
                //判断结点所有的邻接边的等级，也必须要等于stdkind
                //如果连接边中有3个以上边的等级等于stdkind，则这个点就是需要的
                adjlines = tmpdt.Rows[i]["line_adj_id"].ToString();
                adj_lines = adjlines.Split('_');
                tmpcount = 0;
                for (int j = 0; j < adj_lines.Length;j++ )
                {
                    tmpline = adj_lines[j];
                    sqlstr = "select kind from " + layerName + "_newlines" + " " + "where gid=" + tmpline;
                    tmplinedt = postDB.DoQueryEx(sqlstr);
                    tmpkind=Convert.ToInt32(tmplinedt.Rows[0]["kind"].ToString());
                    if (tmpkind==1)
                    {
                        tmpcount++;
                    }
                }
                if (tmpcount >= 3)
                {
                    //所有邻接边的等级和stdkind相同
                    point_id = tmpdt.Rows[i]["vertex_id"].ToString();
                    txtgeom = tmpdt.Rows[i]["geom"].ToString();
                    ctrlsubgraph =GetCtrlArea(point_id,txtgeom);

                    sqlstr = "INSERT INTO" + " " + tablename + " " + "(point_id,kind,controlsubgraph,geom) values (" 
                                + point_id + "," + tmpdt.Rows[i]["kind"].ToString() + ",'" + ctrlsubgraph + "'," + "st_geomfromtext('" + txtgeom + "', 4326)" + ")";
                    postDB.ExecNonQuery(sqlstr);
                } 
                else
                {
                    //邻接边包含等级较低的
                     continue;
                }
            }

            //表示每个分区所有的入口点/出口点
            string ftablename;
            string subgraphnum, entrypoint, resultTable;
            DataTable subgraphTable, entrypointTable;
            string[] subgraphs;
            List<string> subNums;

            ftablename = tablename;
            resultTable = layerName + "_entrypoint_subgraph";
            sqlstr = "create table " + resultTable + " " + "(subnum integer,entrypoint text)";
            postDB.CheckCreateTable(resultTable, sqlstr);
            sqlstr = "select distinct subgraphnum from " + layerName + "_vertex_adjaction order by subgraphnum asc";
            subgraphTable = postDB.DoQueryEx(sqlstr);
            sqlstr = "select * from " + ftablename;
            entrypointTable = postDB.DoQueryEx(sqlstr);

            for (int m = 1; m < subgraphTable.Rows.Count; m++)
            {
                entrypoint = "";
                subgraphnum = subgraphTable.Rows[m]["subgraphnum"].ToString();

                for (int n = 0; n < entrypointTable.Rows.Count;n++ )
                {
                    subNums = new List<string>();
                    ctrlsubgraph = entrypointTable.Rows[n]["controlsubgraph"].ToString();
                    subgraphs = ctrlsubgraph.Split('_');
                    subNums.AddRange(subgraphs);

                    if (subNums.Contains(subgraphnum))
                    {
                        point_id = entrypointTable.Rows[n]["point_id"].ToString();
                        entrypoint += point_id + "_";
                    }
                }
                entrypoint = entrypoint.TrimEnd('_');
                sqlstr = "INSERT INTO" + " " + resultTable + " " + "(subnum,entrypoint) values (" + subgraphnum + ",'" + entrypoint + "')";
                postDB.ExecNonQuery(sqlstr);
            }
            GvwData.DataSource = GetData(resultTable);
        }

        //获取一个入口点的控制区域编号
        private string GetCtrlArea(string pointId,string geom)
        {
            //操作对象是level2_final_entrypoint
            //操作对象为level1_polygon图层，通过点的缓冲区与现有多边形的相交关系，可以求出一个点的控制区域
            string tmpPoint,sqlstr,tmpPolygon,subnum,isContain,ctrlsubnum;
            DataTable tmpdt,tmpResult;

            ctrlsubnum = "";
            //获取点坐标
            //sqlstr = "select st_astext(geom) as point from " + layerName + "_entrypoint" + " " + "where point_id=" + pointId;
            //tmpdt = postDB.DoQueryEx(sqlstr);
            ////此处没有进行异常判断
            //tmpPoint = tmpdt.Rows[0]["point"].ToString();
            tmpPoint = geom;
            sqlstr = "select st_astext(the_geom) as polygon,subnum from level1_polygon";
            tmpdt = postDB.DoQueryEx(sqlstr);
            //可以考虑为多边形建立索引
            for (int i = 0; i < tmpdt.Rows.Count;i++ )
            {
                //判断多边形与点的缓冲区是否相交，st_intersect，缓冲区的半径为meter，所以需要将比较的几何对象进行投影变换
                tmpPolygon = tmpdt.Rows[i]["polygon"].ToString();
                //此处需要测试
                sqlstr = "select st_intersects(st_buffer(st_transform(st_geomfromtext('" + tmpPoint + "', 4326),2345),500),"
                            + "st_transform(st_geomfromtext('" + tmpPolygon + "', 4326),2345)) as iscontain";
                tmpResult = postDB.DoQueryEx(sqlstr);
                isContain = tmpResult.Rows[0]["iscontain"].ToString();
                if (isContain.ToLower()=="true")
                {
                    subnum = tmpdt.Rows[i]["subnum"].ToString();
                    ctrlsubnum += subnum + "_";
                }
            }
            ctrlsubnum=ctrlsubnum.TrimEnd('_');
            return ctrlsubnum;
        }

        //为多边形图层中的多边形编号
        private void PolygonNum()
        {
            //操作对象是level1_polygon图层，表示编号的字段为subnum
            //先检测字段是否存在
            string tablename, columnName, sqlstr;
            DataTable dtpoint,dtpolygon;

            tablename = "level1_polygon";
            columnName = "subnum";
            if (IsColumnExist(tablename, columnName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column" + " " + columnName + " " + "cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column" + " " + columnName + " " + "integer";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column" + " " + columnName + " " + "integer";
                postDB.ExecNonQuery(sqlstr);
            }

            //判断已分区的点和多边形的关系
            //使用st_contains或st_within均可，两个的语义是相反的，要求进行判断的两个geometry具有相同的srid
            //获取分区点的唯一编号
            sqlstr = "select distinct on (subgraphnum) subgraphnum,st_astext(geom) as points from " + layerName + "_vertex_adjaction" + " " + 
                          "where subgraphnum !=0 order by subgraphnum,points";
            dtpoint = postDB.DoQueryEx(sqlstr);
            //获取多边形
            sqlstr = "select gid,st_astext(the_geom) as polygon from " + tablename + " " + "order by gid,polygon";
            dtpolygon = postDB.DoQueryEx(sqlstr);

            string tmpPoint, tmpPolygon,subgraphnum,gidpolygon,tmpresult;
            DataTable containresult;

            //循环判断
            for (int i = 0; i < dtpoint.Rows.Count;i++ )
            {
                tmpPoint = dtpoint.Rows[i]["points"].ToString();
                subgraphnum = dtpoint.Rows[i]["subgraphnum"].ToString();
                for (int j = 0; j < dtpolygon.Rows.Count;j++ )
                {
                    tmpPolygon = dtpolygon.Rows[j]["polygon"].ToString();
                    //知道错误的原因，是因为传入的text必须需要''引起来，fc
                    sqlstr = "select st_contains(st_geomfromtext('" + tmpPolygon + "',4326),st_geomfromtext('" + tmpPoint + "',4326)) as iscontains";
                    //sqlstr="select st_contains(polygon,point) as iscontains from (select st_geomfromtext(" + tmpPolygon + ",4326) as polygon,st_geomfromtext(" + tmpPoint + ",4326) as point)";
                    containresult = postDB.DoQueryEx(sqlstr);
                    tmpresult = containresult.Rows[0]["isContains"].ToString();
                    if (tmpresult.ToLower() =="true")
                    {
                        //更新多边形的信息
                        gidpolygon=dtpolygon.Rows[j]["gid"].ToString();
                        sqlstr = "update " + tablename + " " + "set subnum=" + subgraphnum + " " + "where gid=" + gidpolygon;
                        postDB.ExecNonQuery(sqlstr);
                    }
                }
            }
        }

        private void NewlinesNum()
        {
            //操作对象是newlines图层，表示分区编号的字段为subnum
            //先检测字段是否存在
            string tablename, columnName, sqlstr,ftablename;
            DataTable newlinesTable,tmpTable;
            string gid;
            //表示线段的两个端点的kind值
            string start, end;
            string tmpSubNum;
            string sSubNum, eSubNum;

            tablename = layerName+ "_newlines";
            columnName = "subnum";
            if (IsColumnExist(tablename, columnName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column" + " " + columnName + " " + "cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column" + " " + columnName + " " + "integer";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column" + " " + columnName + " " + "integer";
                postDB.ExecNonQuery(sqlstr);
            }

            sqlstr = "select gid,source,target from " + tablename;
            newlinesTable = postDB.DoQueryEx(sqlstr);

            ftablename = layerName + "_vertex_adjaction";
            for (int i = 0; i < newlinesTable.Rows.Count;i++ )
            {
                gid = newlinesTable.Rows[i]["gid"].ToString();
                //获取两个端点的kind，比较是否相等
                start = newlinesTable.Rows[i]["source"].ToString();
                end = newlinesTable.Rows[i]["target"].ToString();
                sqlstr = "select kind,subgraphnum from " + ftablename + " " + "where vertex_id=" + start;
                tmpTable = postDB.DoQueryEx(sqlstr);
                sSubNum = tmpTable.Rows[0]["subgraphnum"].ToString();
                sqlstr = "select kind,subgraphnum from " + ftablename + " " + "where vertex_id=" + end;
                tmpTable = postDB.DoQueryEx(sqlstr);
                eSubNum = tmpTable.Rows[0]["subgraphnum"].ToString();

                if (sSubNum.Equals(eSubNum))
                {
                    //line的两个端点分区号相同,证明位于某个分区内部
                    sqlstr = "update " + tablename + " " + "set " + columnName + " " + "=" + sSubNum + " " + "where gid=" + gid;
                    postDB.ExecNonQuery(sqlstr);

                } 
                else
                {
                    //line的两个端点分区号不同,获取较大的那个分区号
                    tmpSubNum=GetMaxSubNum(sSubNum, eSubNum);
                    sqlstr = "update " + tablename + " " + "set " + columnName + " " + "=" + tmpSubNum + " " + "where gid=" + gid;
                    postDB.ExecNonQuery(sqlstr);
                }

            }

        }

        private string GetMaxSubNum(string subnum1, string subnum2)
        {
            int num1, num2;
            num1 = int.Parse(subnum1);
            num2 = int.Parse(subnum2);
            if (num1>num2)
            {
                return subnum1;
            } 
            else
            {
                return subnum2;
            }
        }

        //private void btnControlArea_Click(object sender, EventArgs e)
        //{
        //    PolygonNum();
        //    //操作对象是entrypoint表
        //    string sqlstr,ctrlsubgraph,tmpgid;
        //    DataTable tmpdt;

        //    sqlstr = "select point_id from " + layerName + "_entrypoint";
        //    tmpdt = postDB.DoQueryEx(sqlstr);
        //    for (int i = 0; i < tmpdt.Rows.Count;i++ )
        //    {
        //        tmpgid = tmpdt.Rows[i]["point_id"].ToString();
        //        ctrlsubgraph = GetCtrlArea(tmpgid);
        //        //将结果更新到表中
        //        sqlstr = "update " + layerName + "_entrypoint" + " " + "set controlsubgraph='" + ctrlsubgraph + "'" + " " + "where point_id=" + tmpgid;
        //        postDB.ExecNonQuery(sqlstr);
        //    }
        //    //测试处理结果
        //    GvwData.DataSource = GetData(layerName + "_entrypoint");
        //}

        private void btnLineAdjaction_Click(object sender, EventArgs e)
        {
            //分别表示起始结点和终止结点关联的边
            //新建一个表level1_final_line_adjaction,字段包括line_id,start_adj,end_adj,geom
            string tablename, sqlstr;
            DataTable tmpdt;

            tablename = layerName + "_line_adjaction";
            sqlstr = "create table" + " " + tablename + " (line_id integer,start integer,end integer,start_adj text,end_adj text,geom geometry)";
            CheckCreateTable(tablename, sqlstr);
            sqlstr = "select gid,source,target,st_astext(geom) as line from " + layerName + "_newlines";
            tmpdt = postDB.DoQueryEx(sqlstr);

            string tmpid,tmpsource,tmptarget,tmpgeom;
            string startadj, endadj;
            DataTable dt_record;
            //string[] adjline;
            //建立邻接表时表中source和target列已经有索引，此处不需要再建立
            for (int i = 0; i < tmpdt.Rows.Count;i++ )
            {
                startadj = "";
                endadj = "";
                tmpid = tmpdt.Rows[i]["gid"].ToString();
                tmpsource = tmpdt.Rows[i]["source"].ToString();
                tmptarget = tmpdt.Rows[i]["target"].ToString();
                tmpgeom = tmpdt.Rows[i]["line"].ToString();
                //查找关联的边
                //起始结点关联边的信息
                sqlstr = "select gid from " + " " + layerName + "_newlines" + " where gid !=" + tmpid + " and source=" + tmpsource + " or target=" + tmpsource;
                dt_record = postDB.DoQueryEx(sqlstr);
                //会出现查询结果为空的现象，对应不与其他道路连接的端点
                if (dt_record.Rows.Count>0)
                {
                    for (int j = 0; j < dt_record.Rows.Count; j++)
                    {
                        startadj += dt_record.Rows[j]["gid"].ToString() + "_";
                    }
                    //还需要对字符串进行处理
                    startadj = startadj.TrimEnd('_');
                }
                //终止结点关联边的信息
                sqlstr = "select gid from " + " " + layerName + "_newlines" + " where source=" + tmptarget + " or target=" + tmptarget + " " + "and gid !=" + tmpid;
                dt_record = postDB.DoQueryEx(sqlstr);
                //会出现查询结果为空的现象，对应不与其他道路连接的端点
                if (dt_record.Rows.Count > 0)
                {
                    for (int j = 0; j < dt_record.Rows.Count; j++)
                    {
                        endadj += dt_record.Rows[j]["gid"].ToString() + "_";
                    }
                    //还需要对字符串进行处理
                    endadj = endadj.TrimEnd('_');
                }
               //将信息写入表中
                sqlstr = "INSERT INTO" + " " + tablename + " " + "(line_id,start,end,start_adj,end_adj,geom) values ("
                                 + tmpid + ","+ tmpsource + "," + tmptarget + ",'" + startadj + "','" + endadj + "'," + "st_geomfromtext('" + tmpgeom + "', 4326)" + ")";
                postDB.ExecNonQuery(sqlstr);

            }
            GvwData.DataSource = GetData(tablename);
        }

        private void btnPolygonize_Click(object sender, EventArgs e)
        {
            //操作的对象是level1_final_vertex_adjaction表
            //新建count字段表示一条边使用的次数，当所有边count数为2时，构建多边形完成
            string sqlstr, columnName, tablename;
            DataTable tmpdt;
            int count;

            //建立存储多边形的表
            tablename = layerName + "_polygon";
            sqlstr = "create table" + " " + tablename + " (gid integer,lines text,geom geometry)";
            CheckCreateTable(tablename, sqlstr);
            //建立临时表，存储每条路段被标记的信息
            tablename = layerName + "_count";
            sqlstr = "create table" + " " + tablename + " as select gid from " + layerName + "_newlines";
            CheckCreateTable(tablename, sqlstr);
            columnName = "count";
            if (IsColumnExist(tablename, columnName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column" + " " + columnName + " " + "cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column" + " " + columnName + " " + "integer";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + " " + "=0";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column" + " " + columnName + " " + "integer";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + " " + "=0";
                postDB.ExecNonQuery(sqlstr);
            }
            tablename = layerName + "_vertex_adjaction";
            sqlstr = "select vertex_id,line_adj_id from " + tablename + " " + "order by vertex_id,line_adj_id asc";
            tmpdt=postDB.DoQueryEx(sqlstr);

            string vertexid, endadj, tmpgeom, curline, curvertex, nextvertex, tmpendadj, tmppoint, tmpline, polygonGeom,nextpoint,lines;
            DataTable tmpResult,linetxt;
            string[] tmplines;
            List<string> unMarkLines=new List<string>();
            List<string> polygon = new List<string>();
            bool isReverse;
            List<string> tmpMarkLines;
            string angleMax, angleMin;
            int linelength,tmpIndex;
            
            //对所有的边进行遍历
            //针对点-边拓扑关系
            for (int i = 0; i < tmpdt.Rows.Count;i++ )
            {
                lines = "";
                count = 0;
                polygonGeom = "";
                tmpMarkLines = new List<string>();
                angleMax = "";
                angleMin = "";

                unMarkLines.Clear();
                vertexid = tmpdt.Rows[i]["vertex_id"].ToString();
                endadj = tmpdt.Rows[i]["line_adj_id"].ToString();
                unMarkLines=IsMarkAll(endadj);
                while (unMarkLines.Count>0)
                {
                    count++;
                    //存在尚未进行标记的边
                    //取出方位角最小的边
                    curline = unMarkLines[0];
                    curvertex = vertexid;
                    unMarkLines.RemoveAt(0);
                    polygon.Clear();
                    //将出发的vertex记录下来，便于判断读入的线段是哪个方向的
                    polygon.Add(vertexid);
                    //将组成多边形的线段添加进来
                    polygon.Add(curline);
                    //开始查询,获取当前线段的另外一个结点
                    nextvertex = GetOtherEndPoint(curline, curvertex);
                    while (nextvertex !=vertexid)
                    {
                        curvertex = nextvertex;
                        //确定下一条线段
                        sqlstr = "select line_adj_id from " + tablename + " " + "where vertex_id="+ curvertex;
                        tmpResult = postDB.DoQueryEx(sqlstr);
                        tmpendadj = tmpResult.Rows[0]["line_adj_id"].ToString();
                        tmplines = tmpendadj.Split('_');
                        angleMin = tmplines[0];
                        angleMax = tmplines[tmplines.Length - 1];
                        //此处为左转算法的关键判断
                        if (curline.Equals(angleMin))
                        {
                            //当前线段是最小方位角，则去方位角最大的线段作为下一条搜索线段
                            curline = angleMax;
                        } 
                        else
                        {
                            //如果不是最小方位角，则取次小于curline的线段作为下一条搜索线段
                            for (int j = 0; j < tmplines.Length;j++ )
                            {
                                if (tmplines[j].Equals(curline))
                                {
                                    curline = tmplines[j - 1];
                                }
                            }
                        }
                        polygon.Add(curline);
                        nextvertex = GetOtherEndPoint(curline, curvertex);
                    }
                    //将组成多边形的边连接起来生成多边形
                    //需要判断边的方向，判断list中的起始点是否为下一条边的起始点，是的话则可以直接使用，否则需要将边的坐标reverse一下
                    for (int j = 1; j < polygon.Count;j++ )
                    {
                        tmppoint = polygon[0];
                        if (j==1)
                        {
                            tmpline = polygon[j];
                            lines += tmpline + "_";
                            isReverse = IsNeedRevese(tmpline, tmppoint);
                            if (isReverse)
                            {
                                sqlstr = "select st_astext(st_reverse(geom)) as geom from " + layerName + "_newlines" + " " + "where gid=" + tmpline;
                                linetxt = postDB.DoQueryEx(sqlstr);
                                tmpgeom = linetxt.Rows[0]["geom"].ToString();
                                //把multilinestring替换为linestring
                                tmpgeom = tmpgeom.Substring(5, tmpgeom.Length -5);
                                polygonGeom = tmpgeom;
                            } 
                            else
                            {
                                sqlstr = "select st_astext(geom) as geom from " + layerName + "_newlines" + " " + "where gid=" + tmpline;
                                linetxt = postDB.DoQueryEx(sqlstr);
                                tmpgeom = linetxt.Rows[0]["geom"].ToString();
                                tmpgeom = tmpgeom.Substring(5, tmpgeom.Length - 5);
                                polygonGeom = tmpgeom;
                            }
                            //对使用的线段进行标记
                            UpdateLineCount(tmpline);
                        } 
                        else
                        {
                            tmpline = polygon[j];
                            lines += tmpline + "_";
                            nextpoint = GetOtherEndPoint(tmpline, tmppoint);
                            isReverse = IsNeedRevese(tmpline, nextpoint);
                            if (isReverse)
                            {
                                sqlstr = "select st_astext(st_reverse(geom)) as geom from " + layerName + "_newlines" + " " + "where gid=" + tmpline;
                                linetxt = postDB.DoQueryEx(sqlstr);
                                tmpgeom = linetxt.Rows[0]["geom"].ToString();
                                linelength = tmpgeom.Length;
                                tmpgeom = "," + tmpgeom.Substring(17, linelength - 19);
                                //将字符串插入到索引位置之后
                                polygonGeom=polygonGeom.Insert(polygonGeom.LastIndexOf(')') - 1, tmpgeom);
                            } 
                            else
                            {
                                sqlstr = "select st_astext(geom) as geom from " + layerName + "_newlines" + " " + "where gid=" + tmpline;
                                linetxt = postDB.DoQueryEx(sqlstr);
                                tmpgeom = linetxt.Rows[0]["geom"].ToString();
                                //索引值有问题，需要排查
                                //真是粗心啊，第二个参数是选取的字串的长度，不是截止出的索引值
                                linelength = tmpgeom.Length;
                                tmpgeom = "," + tmpgeom.Substring(17, linelength - 19);
                                //将字符串插入到索引位置之后
                                polygonGeom=polygonGeom.Insert(polygonGeom.LastIndexOf(')') - 1, tmpgeom);
                            }
                            nextpoint = GetOtherEndPoint(tmpline, nextpoint);
                            UpdateLineCount(tmpline);
                        }
                        
                    }
                    lines = lines.TrimEnd('_');
                    //此处必须要是linestring，multilinestring不可以貌似
                    //linestring是一个括号，multilinestring是两个，fx
                    tmpIndex = polygonGeom.IndexOf('(');
                    polygonGeom = polygonGeom.Remove(tmpIndex, 1);
                    tmpIndex = polygonGeom.LastIndexOf(')');
                    polygonGeom = polygonGeom.Remove(tmpIndex, 1);
                    //插入
                    sqlstr = "INSERT INTO " + layerName + "_polygon" + " " + "(gid,lines,geom) values(" + count.ToString() + ",'" + lines + "'," + "st_makepolygon(" + "st_geomfromtext('" + polygonGeom + "', 4326))";
                    postDB.ExecNonQuery(sqlstr);
                }
            }
            GvwData.DataSource = GetData(layerName + "_polygon");

        }

        /// <summary>
        /// 更新线段的count数，进行+1操作
        /// </summary>
        /// <param name="tmpline"></param>
        private void UpdateLineCount(string tmpline)
        {
            string sqlstr, tablename, count;
            DataTable tmpdt;

            tablename=layerName+"_count";
            sqlstr = "select count from " + tablename + " " + "where gid=" + tmpline;
            tmpdt = postDB.DoQueryEx(sqlstr);

            count = tmpdt.Rows[0]["count"].ToString();
            if (count.Equals("0"))
            {
                count = "1";
                sqlstr = "update " + tablename + " " + "set count=" + count;
                postDB.ExecNonQuery(sqlstr);
            } 
            else if(count.Equals("1"))
            {
                count = "2";
                sqlstr = "update " + tablename + " " + "set count=" + count;
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                //出现此种情况表示算法有错误
                return;
            }
        }

        /// <summary>
        /// 线段是否需要reverse
        /// </summary>
        /// <param name="tmpline">当前线段</param>
        /// <param name="tmppoint">起点</param>
        /// <returns>需要返回true，否则返回false</returns>
        private bool IsNeedRevese(string tmpline, string tmppoint)
        {
            //通过判断输入的点是否为输入线段的起始结点确定是否需要reverse
            string sqlstr, tmpSource;
            DataTable tmpdt;

            sqlstr = "select source from " + layerName + "_newlines" + " " + "where gid=" + tmpline;
            tmpdt=postDB.DoQueryEx(sqlstr);
            tmpSource=tmpdt.Rows[0]["source"].ToString();
            if (tmppoint.Equals(tmpSource))
            {
                return false;
            } 
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 获取当前线段的另一个端点编号
        /// </summary>
        /// <param name="curline">当前线段的gid</param>
        /// <param name="vertexid">其中一个端点编号</param>
        /// <returns>另一个端点编号</returns>
        private string GetOtherEndPoint(string curline, string vertexid)
        {
            string sqlstr, tmpSource, tmpTarget;
            DataTable tmpdt;

            sqlstr="select source,target from " + layerName + "_newlines" + " " + "where gid=" + curline;
            tmpdt=postDB.DoQueryEx(sqlstr);
            tmpSource=tmpdt.Rows[0]["source"].ToString();
            tmpTarget=tmpdt.Rows[0]["target"].ToString();

            if (tmpSource.Equals(vertexid))
            {
                return tmpTarget;
            }
            else
            {
                return tmpSource;
            }
        }


        private List<string> IsMarkAll(string endadj)
        {
            //判断点的endadj是否都已经被标记过，要求count<2，
            //如果有未标记，则返回list，否则返回null
            List<string> lines;
            string tablename,sqlstr;
            int tmpcount;
            string[] adjlines;
            DataTable tmpdt;

            lines = new List<string>();
            tablename = layerName + "_count";
            adjlines = endadj.Split('_');
            for (int i = 0; i < adjlines.Length;i++ )
            {
                sqlstr = "select count from " + tablename + " " + "where gid=" + adjlines[i];
                tmpdt = postDB.DoQueryEx(sqlstr);
                tmpcount =Convert.ToInt32(tmpdt.Rows[0]["count"].ToString());
                if (tmpcount <2)
                {
                    //未被标记过
                    lines.Add(adjlines[i]);
                }
            }
            return lines;

        }

        private void btnUpdateVertexAdj_Click(object sender, EventArgs e)
        {
            //目标是更新level1的点-边 拓扑关系表，将结点所连接的边按照计算方位角排序并存储
            string sqlstr,tablename,vertexId,adjline,linegeomtxt,pointgeomtxt,tmplineid,sortAdjLines;
            DataTable tmpdt,tmpline;
            string[] lines;
            string[] keyvalues;
            double[] angles;
            double angle;
            //判断当前vertex是否为邻接边的endpoint
            bool isEndPoint;
            //存储对应的线的编号和方位角
            Hashtable ht;
            string tmpTarget;

            isEndPoint = false;
            tablename=layerName+"_vertex_adjaction";
            sqlstr = "select vertex_id,line_adj_id,st_astext(geom) as geom from " + tablename + " " + "order by vertex_id,line_adj_id,geom";
            tmpdt = postDB.DoQueryEx(sqlstr);
            for (int i = 0; i < tmpdt.Rows.Count;i++ )
            {
                sortAdjLines="";
                ht = new Hashtable();
                vertexId = tmpdt.Rows[i]["vertex_id"].ToString();
                adjline = tmpdt.Rows[i]["line_adj_id"].ToString();
                pointgeomtxt=tmpdt.Rows[i]["geom"].ToString();
                lines = adjline.Split('_');
                for (int j = 0; j < lines.Length;j++ )
                {
                    tmplineid = lines[j];
                    //点-边表中的line_id对应于newline中的gid，均从0开始
                    sqlstr = "select target,st_astext(geom) as geom from " + " " + layerName + "_newlines" + " " + "where gid=" + tmplineid;
                    tmpline = postDB.DoQueryEx(sqlstr);
                    linegeomtxt = tmpline.Rows[0]["geom"].ToString();
                    tmpTarget = tmpline.Rows[0]["target"].ToString();
                    if (tmpTarget ==vertexId)
                    {
                        isEndPoint = true;
                    }
                    angle=CalcAngle(pointgeomtxt, linegeomtxt,isEndPoint);
                    ht.Add(tmplineid, angle);
                }
                keyvalues = new string[ht.Count];
                angles = new double[ht.Count];
                ht.Keys.CopyTo(keyvalues, 0);
                ht.Values.CopyTo(angles, 0);
                Array.Sort(angles, keyvalues);
                for (int k = 0; k < keyvalues.Length;k++ )
                {
                    sortAdjLines += keyvalues[k] +"_" ;
                }
                sortAdjLines=sortAdjLines.TrimEnd('_');
                sqlstr = "update " + tablename + " " + "set line_adj_id='" + sortAdjLines + "'" + " " + "where vertex_id="+ vertexId;
                postDB.ExecNonQuery(sqlstr);
            }
            GvwData.DataSource = GetData(tablename);
        }

        /// <summary>
        /// 计算一条线段的方位角
        /// </summary>
        /// <param name="pointgeomtxt">起始点</param>
        /// <param name="linegeomtxt">与起始点连接的最近的点</param>
        /// <param name="isEndPoint">起始点是否为邻接边的终止结点</param>
        /// <returns>方位角</returns>
        private double CalcAngle(string pointgeomtxt, string linegeomtxt,bool isEndPoint)
        {
            //需要从文本信息中解析出点的坐标
            //提取出点信息，使用自定义结构体存储
            double dx, dy,angle;
            PostGISPoint startPoint, endPoint;
            startPoint = GetPointFromTxt(pointgeomtxt);
            endPoint = GetPointFromLine(linegeomtxt,isEndPoint);
            dx = endPoint.x - startPoint.x;
            dy = endPoint.y - startPoint.y;
            if (dx.Equals(double.Parse("0.0")) && dy>0)
            {
                angle = 90.0;
            }
            else if (dx.Equals(double.Parse("0.0")) && dy < 0)
            {
                angle = 270.0;
            }
            else if (dx>0 && dy >=0)
            {
                //第一象限
                angle = Math.Atan(dy / dx);
            }
            else if (dx > 0 && dy < 0)
            {
                //第四象限
                angle = 360 - Math.Atan(Math.Abs(dy / dx));
            }
            else if (dx < 0 && dy >= 0)
            {
                //第二象限
                angle = 180 - Math.Atan(Math.Abs(dy / dx));
            }
            else if (dx < 0 && dy <= 0)
            {
                //第三象限
                angle = 180 + Math.Atan(Math.Abs(dy / dx));
            }
            else
            {
                //此时dx和dy均为0，不存在此种
                angle=0;
            }
            return angle;
        }


        /// <summary>
        /// 根据线的wkt提出点坐标
        /// </summary>
        /// <param name="vertexid">起始点id</param>
        /// <param name="linegeomtxt">所连接边的wkt</param>
        /// <param name="isEndPoint">起始点是否为线段的终止结点</param>
        /// <returns></returns>
        private PostGISPoint GetPointFromLine(string linegeomtxt,bool isEndPoint)
        {
            PostGISPoint point;
            string strpoint;
            string[] tmppoint,tmpXY;
            tmppoint = linegeomtxt.Split(',');
            point = new PostGISPoint();
            //根据是否为端点，分别取不同的点
            if (isEndPoint)
            {
                //需要对组成线段的点数进行判断，如果为2，则直接获取
                if (tmppoint.Length==2)
                {
                    //此时需要取线段的第一个点，即起始点
                    strpoint = tmppoint[0].Substring(18);
                    tmpXY = strpoint.Split(' ');
                    point.x = double.Parse(tmpXY[0]);
                    point.y = double.Parse(tmpXY[1]);
                } 
                else
                {
                    //需要提取倒数第二个点
                    strpoint = tmppoint[tmppoint.Length - 2];
                    tmpXY = strpoint.Split(' ');
                    point.x = double.Parse(tmpXY[0]);
                    point.y = double.Parse(tmpXY[1]);
                }
                
            } 
            else
            {
                if (tmppoint.Length == 2)
                {
                    //此时需要取线段的第二个点，即终止结点
                    strpoint = tmppoint[1].Substring(0,tmppoint[1].Length-3);
                    tmpXY = strpoint.Split(' ');
                    point.x = double.Parse(tmpXY[0]);
                    point.y = double.Parse(tmpXY[1]);
                } 
                else
                {
                    //提取第二个点
                    strpoint = tmppoint[1];
                    tmpXY = strpoint.Split(' ');
                    point.x = double.Parse(tmpXY[0]);
                    point.y = double.Parse(tmpXY[1]);
                }
                
            }
            return point;
        }

        /// <summary>
        /// 提取起始点坐标
        /// </summary>
        /// <param name="pointgeomtxt">点对象的wkt</param>
        /// <returns>起始点坐标</returns>
        private PostGISPoint GetPointFromTxt(string pointgeomtxt)
        {
            //"POINT(116.369301237714 39.762246673303)"
            string[] tmp;
            string tmpX,tmpY;
            PostGISPoint tmpPoint;

            tmpPoint = new PostGISPoint();
            tmp = pointgeomtxt.Split(' ');
            tmpX = tmp[0].Substring(6);
            tmpY = tmp[1].Substring(0, tmp[1].Length - 2);
            tmpPoint.x = double.Parse(tmpX);
            tmpPoint.y = double.Parse(tmpY);

            return tmpPoint;
        }


        //设置道路的长度
        //并没有改变几何对象的投影
        private void SetRoadLength(string tablename, string geocolumn,string projectsrid)
        {
            string sqlstr;
            string lengthcolumn = "length";

            if (IsColumnExist(tablename, lengthcolumn))
            {
                sqlstr = "alter table " + tablename + " " + "drop column " + " " + lengthcolumn + " " + " cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + lengthcolumn + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + lengthcolumn + "=st_length(st_transform(" + geocolumn + "," + projectsrid + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + lengthcolumn + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + lengthcolumn + "=st_length(st_transform(" + geocolumn + "," + projectsrid + "))";
                postDB.ExecNonQuery(sqlstr);
            }
        }


        //Astar算法和shootingstar算法添加xy坐标字段
        private void SetCoordinate(string tablename, string geocolumn)
        {
            string sqlstr,columnName;

            columnName = "x1";
            if (IsColumnExist(tablename, columnName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column " + " " + columnName + " " + " cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_x(startpoint(" + geocolumn + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_x(startpoint(" + geocolumn + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            columnName = "y1";
            if (IsColumnExist(tablename, columnName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column " + " " + columnName + " " + " cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_y(startpoint(" + geocolumn + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_y(startpoint(" + geocolumn + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            columnName = "x2";
            if (IsColumnExist(tablename, columnName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column " + " " + columnName + " " + " cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_x(endpoint(" + geocolumn + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_x(endpoint(" + geocolumn + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            columnName = "y2";
            if (IsColumnExist(tablename, columnName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column " + " " + columnName + " " + " cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_y(endpoint(" + geocolumn + "))";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "=st_y(endpoint(" + geocolumn + "))";
                postDB.ExecNonQuery(sqlstr);
            }
        }

        /// <summary>
        /// 为shooting*算法做准备，其中to_cost字段和rule字段可以为空
        /// </summary>
        /// <param name="tablename">表名称</param>
        private void SetCostColumn(string tablename)
        {
            string sqlstr, columnName;

            columnName = "reverse_cost";
            if (IsColumnExist(tablename, columnName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column " + " " + columnName + " " + " cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "length";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "update " + tablename + " " + "set " + columnName + "length";
                postDB.ExecNonQuery(sqlstr);
            }
            columnName = "to_cost";
            if (IsColumnExist(tablename, columnName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column " + " " + columnName + " " + " cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "double precision";
                postDB.ExecNonQuery(sqlstr); 
            }
            columnName = "rule";
            if (IsColumnExist(tablename, columnName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column " + " " + columnName + " " + " cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "text";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + columnName + " " + "text";
                postDB.ExecNonQuery(sqlstr);
            }
        }

        private void UpdateShortestResult(string ftablename,string tablename)
        {
            string geoColumn,sqlstr;
            DataTable tmpdt,tmpresult;

            geoColumn = "geom";

            if (IsColumnExist(tablename, geoColumn))
            {
                sqlstr = "alter table " + tablename + " " + "drop column " + " " + geoColumn + " " + " cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + geoColumn + " " + "geometry";
                postDB.ExecNonQuery(sqlstr);
            }
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column  " + " " + geoColumn + " " + "geometry";
                postDB.ExecNonQuery(sqlstr);
            }
            //结果表中起始点或者终止点，对应的edge_id为-1，需要处理
            tmpdt= GetData(tablename);
            for (int i = 0; i <= tmpdt.Rows.Count - 1; i++)
            {
                string edge_id = tmpdt.Rows[i]["edge_id"].ToString();
                string tmpGeom;
                if (! edge_id.Equals("-1"))
                {
                    sqlstr = "select st_astext(geom) as geomtext from " + " " + ftablename + " " + "where gid=" + edge_id;
                    tmpresult = postDB.DoQueryEx(sqlstr);
                    tmpGeom = tmpresult.Rows[0]["geomtext"].ToString();
                    //这个地方涉及到一个问题，如果已经将newlines中的几何字段进行投影转变？此处处理方式需要改变
                    sqlstr = "update " + " " + tablename + " " + "set" + " " + geoColumn + "=" + "st_geomfromtext('" + tmpGeom + "', 4326) where edge_id=" + edge_id;
                    postDB.ExecNonQuery(sqlstr);
                }                
            }
        }

        private void btnClassical_Click(object sender, EventArgs e)
        {
            string ftablename, start, end, algorithm,tablename;

            ftablename = layerName + "_newlines";
            start = this.textBoxStartPoint.Text.Trim();
            end = this.textBoxEndPoint.Text.Trim();
            algorithm = cbxAlgorithm.SelectedItem.ToString().Trim();
            tablename = "";

            ClassicalAlgorithms ca = new ClassicalAlgorithms(postDB);
            Timing timeRecorder = new Timing();
            switch (algorithm)
            {
                case "Dijkstra":
                    timeRecorder.Start();
                    ca.Dijkstra(ftablename, start, end);
                    tablename = layerName + "_classicaldijkstra";
                    timeRecorder.Stop();
                    TimeCost = timeRecorder.Result();
                    //UpdateShortestResult(ftablename, tablename);
                    break;
                case "Astar":
                    timeRecorder.Start();
                    ca.Astar(ftablename, start, end);
                    tablename = layerName + "_classicalastar";
                    timeRecorder.Stop();
                    TimeCost = timeRecorder.Result();
                    //UpdateShortestResult(ftablename, tablename);
                    break;
                default:
                    break;
            }
            this.txtCostTime.Text = TimeCost.ToString();
            GvwData.DataSource = GetData(tablename);
        }

        private void btnMetisData_Click(object sender, EventArgs e)
        {
            //将level2的数据转为metis能够处理的纯文本格式
            //需要统计图中边的数量，等于newlines表中记录的数目
            string sqlstr, tablename,vertex,adjvertex,tmpVertex;
            string[] tmpAdjVertex;
            DataTable tmpdt,tmpCount,tmpLength;
            int count,gid,vertexCount,edgeCount;

            tablename = layerName + "_newlines";
            sqlstr="select count(*) from " + tablename;
            tmpdt = postDB.DoQueryEx(sqlstr);
            edgeCount = int.Parse(tmpdt.Rows[0]["count"].ToString());

            tablename=layerName+"_vertex_adjaction";
            sqlstr = "select vertex_id,vertex_adj_id from " + tablename + " " + "order by vertex_id asc";
            tmpdt = postDB.DoQueryEx(sqlstr);
            vertexCount = tmpdt.Rows.Count;
                        
            #region 测试代码
            //此处获得的edge值并不等于newlines的记录数，此段代码有问题
            //gid = 0;
            //tablename = layerName + "_edge";
            //sqlstr = "create table " + tablename + " (gid integer,point1 integer,point2 integer)";
            //postDB.CheckCreateTable(tablename, sqlstr);

            //for (int i = 0; i < tmpdt.Rows.Count; i++)
            //{
            //    vertex = tmpdt.Rows[i]["vertex_id"].ToString();
            //    adjvertex = tmpdt.Rows[i]["vertex_adj_id"].ToString();
            //    tmpAdjVertex = adjvertex.Split('_');
            //    for (int j = 0; j < tmpAdjVertex.Length; j++)
            //    {
            //        tmpVertex = tmpAdjVertex[j];
            //        sqlstr = "select count(*) from " + tablename + " where point1=" + vertex + " " + "and point2=" + tmpVertex + " " + "or point1=" + " " + tmpVertex + " " + "and point2=" + vertex;
            //        tmpCount = postDB.DoQueryEx(sqlstr);
            //        count = int.Parse(tmpCount.Rows[0]["count"].ToString());
            //        if (count == 0)
            //        {
            //            gid++;
            //            sqlstr = "insert into " + tablename + " (gid,point1,point2) values(" + gid.ToString() + "," + vertex + "," + tmpVertex + ")";
            //            postDB.ExecNonQuery(sqlstr);
            //        }
            //    }
            //}
            //sqlstr="select count(*) from " + tablename;
            //tmpCount = postDB.DoQueryEx(sqlstr);
            //edgeCount = int.Parse(tmpCount.Rows[0][0].ToString());
#endregion
            
            string fileName = layerName + "_MetisGraph.txt";
            //与bin文件夹在同一目录下
            string filePath = Application.StartupPath + "\\";
            StreamWriter sw;
            
            if (! File.Exists(filePath+fileName))
            {
                sw=File.CreateText(filePath + fileName);
            }
            else
            {
                sw = new StreamWriter(filePath + fileName, false);
            }
            sw.WriteLine(vertexCount.ToString() + " " + edgeCount.ToString() + " " + "001");
            tablename=layerName+"_newlines";

            for (int m = 0; m < vertexCount;m++ )
            {
                int reLength;
                string formatStr,length;
                formatStr="";
                vertex = tmpdt.Rows[m]["vertex_id"].ToString();
                adjvertex = tmpdt.Rows[m]["vertex_adj_id"].ToString();
                tmpAdjVertex = adjvertex.Split('_');
                //要求权值必须是整数
                for (int n = 0; n < tmpAdjVertex.Length;n++ )
                {
                    tmpVertex = tmpAdjVertex[n];
                    sqlstr="select length from " + tablename + " " + "where source=" + vertex + " " + "or target=" + tmpVertex + " " + "and source=" + tmpVertex + " " + "or target=" +vertex;
                    tmpLength=postDB.DoQueryEx(sqlstr);
                    length=tmpLength.Rows[0]["length"].ToString();
                    reLength = (int)double.Parse(length);
                    formatStr +=tmpVertex+ " " + reLength.ToString() + " ";
                }
                formatStr.TrimEnd(' ');
                sw.WriteLine(formatStr);
            }
            sw.Close();
            string algorithmName = this.cbxMetisAlgorithm.SelectedItem.ToString().Trim();
            //检测adj表中是否添加分区字段
            string colName = "metispartition_"+algorithmName;
            //string colName = "metispartition_rb";
            tablename = layerName + "_vertex_adjaction";
            if (postDB.IsColumnExist(tablename, colName))
            {
                sqlstr = "alter table " + tablename + " " + "drop column " + colName + "  cascade";
                postDB.ExecNonQuery(sqlstr);
                sqlstr = "alter table " + tablename + " " + "add column " + colName + "  integer";
                postDB.ExecNonQuery(sqlstr);
            } 
            else
            {
                sqlstr = "alter table " + tablename + " " + "add column " + colName + "  integer";
                postDB.ExecNonQuery(sqlstr);
            }
            //调用gpmetis.exe程序，并将参数传递进去，得到结果后读取文件，将结果写入到adj表中
            //此为可设置参数，此处设置为24
            string npart = this.txtPartNum.Text.Trim();
            string resultFileName = fileName + ".part." + npart;
            //检测结果文件是否存在
            if (File.Exists(filePath+resultFileName))
            {
                File.Delete(filePath + resultFileName);
            } 
            CmdOperation co = new CmdOperation();
            //使用两种方式kway和rb算法进行划分

            string[] cmd = new string[] { };
            switch (algorithmName)
            {
                case "kway":
                    cmd = new string[] { "cd /d " + filePath, "gpmetis " + fileName + " " + npart };
                    break;
                case "rb":
                    cmd = new string[] { "cd /d " + filePath, "gpmetis -ptype=rb " + fileName + " " + npart };
                    break;
                default:
                    break;
            }
            co.ExecuteCmd(cmd);
            //检测文件是否存在，判定是否计算成功
            string subNum;
            int vertexId;
            if (File.Exists(filePath + resultFileName))
            {
                StreamReader sr = new StreamReader(filePath + resultFileName, Encoding.UTF8);
                subNum = sr.ReadLine();
                vertexId=1;
                while (subNum !=null)
                {
                    sqlstr = "update " + " " + tablename + " " + "set" + " " + colName + "="  + subNum + " " + "where vertex_id=" + vertexId;
                    postDB.ExecNonQuery(sqlstr);
                    subNum = sr.ReadLine();
                    vertexId++;
                }
            }

            //计算完成后生成对应的多边形
            ComputePolygon(tablename, npart, algorithmName);
            GvwData.DataSource = GetData(tablename);
            //MessageBox.Show("计算已完成");
        }

        private void ComputePolygon(string tablename, string npart, string algorithmName)
        {
            //生成分区的点的凸包，和另一种分区的多边形进行对比
            string sqlstr, metisColName, geomTxt, resultTable;
            DataTable tmpdt;
            int pNum;

            pNum = Convert.ToInt32(npart);
            metisColName = "metispartition_" + algorithmName;
            //检测结果表是否存在
            resultTable = layerName + "_polygon_metis_" + algorithmName;
            //gid即为所控制的点的分区编号
            sqlstr = "create table " + resultTable + " " + "(gid integer,geom geometry)";
            postDB.CheckCreateTable(resultTable, sqlstr);

            for (int i = 0; i < pNum; i++)
            {
                sqlstr = "select st_astext(st_convexhull(st_collect(geom))) as geom from " + tablename + " " + "where " + metisColName + "=" + i.ToString();
                tmpdt = postDB.DoQueryEx(sqlstr);
                geomTxt = tmpdt.Rows[0]["geom"].ToString();
                sqlstr = "INSERT INTO" + " " + resultTable + " " + "(gid,geom) values (" + i.ToString() + "," + "st_geomfromtext('" + geomTxt + "', 4326))";
                postDB.ExecNonQuery(sqlstr);
            }
        }

        //private void btnMetisPolygon_Click(object sender, EventArgs e)
        //{
        //    //生成分区的点的凸包，和另一种分区的多边形进行对比
        //    string sqlstr, tablename,metisColName,partitionNum,geomTxt,resultTable;
        //    DataTable tmpdt;
        //    int pNum;

        //    tablename=layerName+"_vertex_adjaction";
        //    metisColName="metispartition_" + algorithmName;
        //    //metisColName = "metispartition_rb";
        //    sqlstr = "select count(" + metisColName + ") from " + tablename;
        //    tmpdt=postDB.DoQueryEx(sqlstr);
        //    partitionNum = tmpdt.Rows[0]["count"].ToString();
        //    pNum = int.Parse(partitionNum);

        //    //检测结果表是否存在
        //    resultTable = layerName + "_polygon_metis_" + algorithmName;
        //    //resultTable = layerName + "_polygon_metis_rb";
        //    //gid即为所控制的点的分区编号
        //    sqlstr = "create table " + resultTable + " " + "(gid integer,geom geometry)";
        //    postDB.CheckCreateTable(resultTable, sqlstr);

        //    for (int i = 0; i < pNum;i++ )
        //    {
        //        sqlstr = "select st_astext(st_convexhull(st_collect(geom))) as geom from " + tablename + " " + "where " + metisColName + "=" + i.ToString();
        //        tmpdt = postDB.DoQueryEx(sqlstr);
        //        geomTxt = tmpdt.Rows[0]["geom"].ToString();
        //        sqlstr = "INSERT INTO" + " " + resultTable + " " + "(gid,geom) values (" + i.ToString() + "," + "st_geomfromtext('" + geomTxt + "', 4326))";
        //        postDB.ExecNonQuery(sqlstr);
        //    }
        //    GvwData.DataSource = GetData(resultTable);

        //}

        private void btnHierarchyAlgorthm_Click(object sender, EventArgs e)
        {
            string ftablename, start, end, algorithm, tablename;

            ftablename = layerName + "_newlines";
            start = this.textBoxStartPoint.Text.Trim();
            end = this.textBoxEndPoint.Text.Trim();
            algorithm = cbxAlgorithm.SelectedItem.ToString().Trim();
            tablename = "";

            HierarchyAlgorithms ha = new HierarchyAlgorithms(postDB);
            Timing timeRecorder = new Timing();
            switch (algorithm)
            {
                case "Dijkstra":
                    timeRecorder.Start();
                    ha.Dijkstra(ftablename, start, end);
                    tablename = layerName + "_hierarchydijkstra";
                    timeRecorder.Stop();
                    TimeCost = timeRecorder.Result();
                    UpdateShortestResult(ftablename, tablename);
                    break;
                case "Astar":
                    timeRecorder.Start();
                    ha.Astar(ftablename, start, end);
                    tablename = layerName + "_hierarchyastar";
                    timeRecorder.Stop();
                    TimeCost = timeRecorder.Result();
                    UpdateShortestResult(ftablename, tablename);
                    break;
                default:
                    break;
            }
            this.txtCostTime.Text = TimeCost.ToString();
            GvwData.DataSource = GetData(tablename);
        }

        private void btnBDAlgorithm_Click(object sender, EventArgs e)
        {
            string ftablename, start, end, algorithm, tablename, geoColumn;

            ftablename = layerName + "_newlines";
            start = this.textBoxStartPoint.Text.Trim();
            end = this.textBoxEndPoint.Text.Trim();
            algorithm = cbxBdAlgorithms.SelectedItem.ToString().Trim();
            tablename = "";
            geoColumn = "geom";

            BiHiAlgorithms bh = new BiHiAlgorithms(postDB);
            Timing timeRecorder = new Timing();
            switch (algorithm)
            {
                case "BdDijkstra":
                    timeRecorder.Start();
                    bh.ComputePath(algorithm, ftablename, start, end);
                    tablename = layerName + "_hierarchy_bddijkstra";
                    timeRecorder.Stop();
                    TimeCost = timeRecorder.Result();
                    break;
                case "BdAstar":
                    SetCoordinate(ftablename, geoColumn);
                    timeRecorder.Start();
                    bh.ComputePath(algorithm, ftablename, start, end);
                    tablename = layerName + "_hierarchy_bdastar";
                    timeRecorder.Stop();
                    TimeCost = timeRecorder.Result();
                    break;
                default:
                    break;
            }
            this.txtCostTime.Text = TimeCost.ToString();
            GvwData.DataSource = GetData(tablename);

        }

        #region 测试生成20-30个分区的代码
        //程序中有个问题，如果先执行了kway，之后在执行rb时，会提示说分区文件正在使用，文件的使用没有完全释放
        private void btnMETISTest_Click(object sender, EventArgs e)
        {
            try
            {
                //传递进来分区数，生成相应的分区文件，分区结果更新到level2_newlines_test表中
                string fileName = layerName + "_MetisGraph.txt";
                //与bin文件夹在同一目录下
                string filePath = Application.StartupPath + "\\";
                string tablename = layerName + "_vertex_adjaction_test";
                int start = 21;
                int stop = 30;
                string sqlstr;

                string algorithmName = this.cbxMetisAlgorithm.SelectedItem.ToString().Trim();

                for (int i = start; i < stop; i++)
                {
                    //检测adj表中是否添加分区字段
                    string colName = "metispartition_" + algorithmName + "_" + i.ToString();
                    if (postDB.IsColumnExist(tablename, colName))
                    {
                        sqlstr = "alter table " + tablename + " " + "drop column " + colName + "  cascade";
                        postDB.ExecNonQuery(sqlstr);
                        sqlstr = "alter table " + tablename + " " + "add column " + colName + "  integer";
                        postDB.ExecNonQuery(sqlstr);
                    }
                    else
                    {
                        sqlstr = "alter table " + tablename + " " + "add column " + colName + "  integer";
                        postDB.ExecNonQuery(sqlstr);
                    }
                    //调用gpmetis.exe程序，并将参数传递进去，得到结果后读取文件，将结果写入到adj表中
                    string resultFileName = fileName + ".part." + i.ToString();
                    //检测结果文件是否存在
                    if (File.Exists(filePath + resultFileName))
                    {
                        File.Delete(filePath + resultFileName);
                    }
                    CmdOperation co = new CmdOperation();
                    //使用两种方式kway和rb算法进行划分

                    string[] cmd = new string[] { };
                    switch (algorithmName)
                    {
                        case "kway":
                            cmd = new string[] { "cd /d " + filePath, "gpmetis " + fileName + " " + i.ToString() };
                            break;
                        case "rb":
                            cmd = new string[] { "cd /d " + filePath, "gpmetis -ptype=rb " + fileName + " " + i.ToString() };
                            break;
                        default:
                            break;
                    }
                    co.ExecuteCmd(cmd);
                    //检测文件是否存在，判定是否计算成功
                    string subNum;
                    int vertexId;
                    if (File.Exists(filePath + resultFileName))
                    {
                        StreamReader sr = new StreamReader(filePath + resultFileName, Encoding.UTF8);
                        subNum = sr.ReadLine();
                        vertexId = 1;
                        while (subNum != null)
                        {
                            sqlstr = "update " + " " + tablename + " " + "set" + " " + colName + "=" + subNum + " " + "where vertex_id=" + vertexId;
                            postDB.ExecNonQuery(sqlstr);
                            subNum = sr.ReadLine();
                            vertexId++;
                        }
                    }
                    ComputePolygonTest(tablename, i.ToString(), algorithmName);
                    string Max, Min, Median;
                    ComputeStats(tablename, i.ToString(), algorithmName, out Max, out Min, out Median);
                    fileName = algorithmName + "_stats_result.txt";
                    //与bin文件夹在同一目录下
                    filePath = Application.StartupPath + "\\";
                    StreamWriter sw;
                    if (!File.Exists(filePath + fileName))
                    {
                        sw = File.CreateText(filePath + fileName);
                    }
                    else
                    {
                        sw = new StreamWriter(filePath + fileName, true);
                    }
                    sw.WriteLine("{0}\t{1}\t{2}\t{3}", i.ToString(), Max, Min, Median);
                    sw.Flush();
                    sw.Close();
                }
                MessageBox.Show("计算结束");
            }
            catch (System.Exception ex)
            {
            	
            }
            finally
            {
                
            }
           
        }

        private void ComputeStats(string tablename, string npart, string algorithmName, out string Max, out string Min, out string Median)
        {
            //对分区结果进行统计，获取分区中结点数的最大值，最小值和中值
            string sqlstr, metisColName;
            DataTable tmpdt;
            int pNum;

            pNum = Convert.ToInt32(npart);
            metisColName = "metispartition_" + algorithmName + "_" + npart;
            sqlstr = "select " + metisColName + ",count(*) from " + tablename + " group by " + metisColName + " " + "order by " + metisColName;
            tmpdt = postDB.DoQueryEx(sqlstr);

            Hashtable ht = new Hashtable();
            string count;
            int tmpCount,tmpMax,tmpMin,tmpMd,sum,mid;
            sum = 0;
            mid = (int)(pNum + 1) / 2;
            count = tmpdt.Rows[0]["count"].ToString();
            tmpCount = int.Parse(count);
            tmpMax = tmpCount;
            tmpMin = tmpCount;
            tmpMd = tmpCount;
            for (int i = 0; i < tmpdt.Rows.Count;i++ )
            {
                count = tmpdt.Rows[i]["count"].ToString();
                tmpCount = int.Parse(count);
                tmpMax = tmpCount > tmpMax ? tmpCount : tmpMax;
                tmpMin = tmpCount < tmpMin ? tmpCount : tmpMin;
                if (!ht.ContainsKey(count))
                {
                    ht.Add(count, 1);
                }
                else
                {
                    sum = (int)ht[count];
                    sum += 1;
                    ht[count] = sum;
                }
            }
            sum = 0;
            foreach (DictionaryEntry de in ht )
            {
                tmpCount = (int)de.Value;
                if (sum < mid && sum + tmpCount >mid)
                {
                    tmpMd = int.Parse(de.Key.ToString());
                    break;
                }
                sum += tmpCount;
            }
            Max = tmpMax.ToString();
            Min = tmpMin.ToString();
            Median = tmpMd.ToString();

        }

        private void ComputePolygonTest(string tablename, string npart, string algorithmName)
        {
            //生成分区的点的凸包，和另一种分区的多边形进行对比
            string sqlstr, metisColName, geomTxt, resultTable;
            DataTable tmpdt;
            int pNum;

            pNum = Convert.ToInt32(npart);
            metisColName = "metispartition_" + algorithmName + "_" + npart;
            //检测结果表是否存在
            resultTable = layerName + "_polygon_metis_" + algorithmName + "_" + npart;
            //gid即为所控制的点的分区编号
            sqlstr = "create table " + resultTable + " " + "(gid integer,geom geometry)";
            postDB.CheckCreateTable(resultTable, sqlstr);

            for (int i = 0; i < pNum; i++)
            {
                sqlstr = "select st_astext(st_convexhull(st_collect(geom))) as geom from " + tablename + " " + "where " + metisColName + "=" + i.ToString();
                tmpdt = postDB.DoQueryEx(sqlstr);
                geomTxt = tmpdt.Rows[0]["geom"].ToString();
                sqlstr = "INSERT INTO" + " " + resultTable + " " + "(gid,geom) values (" + i.ToString() + "," + "st_geomfromtext('" + geomTxt + "', 4326))";
                postDB.ExecNonQuery(sqlstr);
            }
        }

#endregion
        
        

    }
}