using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Collections;

namespace ConnPostSQL
{
    //�����㷨ʵ��
    //��Ҫ�������㷨��Dijkstra�㷨��A*�㷨
    //�뵽һ������㷨ִ���ٶȵķ���
    //���ڵĴ���������ݿ�Ĵ���Ƶ������Ҫ�Ƕ�ȡ�����ڽӱ���ȡ�ߵĳ�����Ϣ�������ɿ��ǽ��ڽӱ������ڴ��ֱ�ӷ��ʣ��ٶȻ������ܶ�
    class ClassicalAlgorithms
    {
        private DB postDB;
        private const double MaxDistance = double.MaxValue;
        public ClassicalAlgorithms(DB outDB)
        {
            postDB = outDB;
        }

        public void Dijkstra(string ftablename, string startpoint, string endpoint)
        {
            //�����ڽӱ�
            //�洢������·���ıߵ����
            //����Ƿ���
            bool[] final;
            double minLength;
            string sqlstr,tablename,tmpVertex,adjLine,tmpLine,tmpLength;
            DataTable VertexTable,tmpLengthTable;
            int totalVertex,start,end,curVertex;
            string[] tmpAdjLine;
            //�洢ÿ����㵽Դ������·��ֵ
            double[] dist;
            //���ĸ����
            int[] prev;

            tablename=GetLayerName(ftablename) + "_vertex_adjaction";
            sqlstr = "select vertex_id,vertex_adj_id,line_adj_id from " + tablename + " " + "order by vertex_id asc";
            VertexTable = postDB.DoQueryEx(sqlstr);
            totalVertex = VertexTable.Rows.Count;

            //��ʼ��
            start = int.Parse(startpoint);
            end = int.Parse(endpoint);
            //��ʼ��֮��Ĭ�ϼ�Ϊfalse
            final = new bool[totalVertex];
            dist = new double[totalVertex];
            //prev�д洢�������Ƚ����С1
            prev = new int[totalVertex];

            for (int n = 0; n < totalVertex;n++ )
            {
                if (! start.Equals(n+1))
                {
                    dist[n] = double.MaxValue;
                    prev[n] = 0;
                    final[n] = false;
                }
            }
            adjLine = VertexTable.Rows[start - 1]["line_adj_id"].ToString();
            tmpAdjLine = adjLine.Split('_');
            //����start�����ӵıߵ�distֵ
            for (int m = 0; m < tmpAdjLine.Length; m++)
            {
                tmpLine = tmpAdjLine[m];
                sqlstr = "select source,target,length from " + ftablename + " where gid=" + tmpLine;
                tmpLengthTable = postDB.DoQueryEx(sqlstr);
                tmpVertex=tmpLengthTable.Rows[0]["source"].ToString();
                tmpLength = tmpLengthTable.Rows[0]["length"].ToString();
                if (startpoint.Equals(tmpVertex))
                {
                    tmpVertex = tmpLengthTable.Rows[0]["target"].ToString();
                    dist[int.Parse(tmpVertex) - 1] = double.Parse(tmpLength);
                    prev[int.Parse(tmpVertex) - 1] = start-1;
                } 
                else
                {
                    dist[int.Parse(tmpVertex) - 1] = double.Parse(tmpLength);
                    prev[int.Parse(tmpVertex) - 1] = start-1;
                }
            }
            dist[start-1] = 0;
            final[start-1] = true;

            //���end��㱻��Ǽ������Ѿ��ҵ�����֮������·��
            while (! final[end-1])
            {
                curVertex = start;
                minLength = double.MaxValue;
                for (int i = 0; i < totalVertex;i++ )
                {
                    if (! final[i] && dist[i]<minLength)
                    {
                        //�˴���õ�curVertex����ʵ�Ľ��idС1
                        curVertex = i+1;
                        minLength = dist[i];
                    }
                }
                final[curVertex-1] = true;
#region �ڽӱ�
                //����ͨ�����������ڴ��еı�ļ�¼��ȡ�����ڽӱ����
                adjLine = VertexTable.Rows[curVertex - 1]["line_adj_id"].ToString();
                tmpAdjLine = adjLine.Split('_');
                double tmpdist;
                for (int j = 0; j < tmpAdjLine.Length; j++)
                {
                    tmpLine = tmpAdjLine[j];
                    sqlstr = "select source,target,length from " + ftablename + " where gid=" + tmpLine;
                    tmpLengthTable = postDB.DoQueryEx(sqlstr);
                    tmpVertex = tmpLengthTable.Rows[0]["source"].ToString();
                    tmpLength = tmpLengthTable.Rows[0]["length"].ToString();
                    if (curVertex.Equals(int.Parse(tmpVertex)))
                    {
                        tmpVertex = tmpLengthTable.Rows[0]["target"].ToString();
                    }
                    //��ǰ���ӵĽ��δ�����
                    if (! final[int.Parse(tmpVertex) - 1])
                    {
                        tmpdist = dist[curVertex - 1] + double.Parse(tmpLength);
                        if (tmpdist < dist[int.Parse(tmpVertex) - 1])
                        {
                            dist[int.Parse(tmpVertex) - 1] = tmpdist;
                            //�˴����Ը���Ϊ����߶ε����ƺͶ�Ӧ�ĳ��ȣ�����ڽ��������ʾ
                            prev[int.Parse(tmpVertex) - 1] = curVertex-1;
                        }
                    }
                }
#endregion
            
            }

            List<int> result;
            int tmp;
            string tmpGeomTxt;

            result = new List<int>();
            result.Add(end);
            tmp = prev[end - 1]+1;
            while (! tmp.Equals(start))
            {
                result.Add(tmp);
                tmp = prev[tmp - 1]+1;
            }
            result.Add(start);
            result.Reverse();
            //�����д�뵽����
            //д������ݰ�����gid,geom,length
            //�������������checkcreatetable
            tablename=GetLayerName(ftablename) + "_classicaldijkstra";
            sqlstr = "create table " + tablename + " (gid integer,length double precision,geom geometry)";
            postDB.CheckCreateTable(tablename, sqlstr);
            for (int i = 0; i < result.Count - 1;i++ )
            {
                int cur, next;
                cur = result[i];
                next = result[i + 1];
                tmpLine = GetLineID(ftablename,cur.ToString(), next.ToString());
                sqlstr = "select length,st_astext(geom) as geomtxt from " + ftablename + " " + "where gid=" + tmpLine;
                tmpLengthTable = postDB.DoQueryEx(sqlstr);
                tmpLength = tmpLengthTable.Rows[0]["length"].ToString();
                tmpGeomTxt = tmpLengthTable.Rows[0]["geomtxt"].ToString();
                sqlstr = "INSERT INTO" + " " + tablename + " " + "(gid,length,geom) values (" + tmpLine + ","
                             + tmpLength + "," + "st_geomfromtext('" + tmpGeomTxt + "', 4326)" + ")";
                postDB.ExecNonQuery(sqlstr);
            }       
            
        }

#region ˫��Dijkstra
        public void BdDijkstra(string ftablename, string startpoint, string endpoint)
        {
            //�����ڽӱ�
            //�洢������·���ıߵ����
            //����Ƿ���
            bool[] final;
            double minLength;
            string sqlstr, tablename, tmpVertex, adjLine, tmpLine, tmpLength;
            DataTable VertexTable, tmpLengthTable;
            int totalVertex, start, end, curVertex;
            string[] tmpAdjLine;
            //�洢ÿ����㵽Դ������·��ֵ
            double[] dist;
            //���ĸ����
            int[] prev;

            tablename = GetLayerName(ftablename) + "_vertex_adjaction";
            sqlstr = "select vertex_id,vertex_adj_id,line_adj_id from " + tablename + " " + "order by vertex_id asc";
            VertexTable = postDB.DoQueryEx(sqlstr);
            totalVertex = VertexTable.Rows.Count;

            //��ʼ��
            start = int.Parse(startpoint);
            end = int.Parse(endpoint);
            //��ʼ��֮��Ĭ�ϼ�Ϊfalse
            final = new bool[totalVertex];
            dist = new double[totalVertex];
            //prev�д洢�������Ƚ����С1
            prev = new int[totalVertex];

            for (int n = 0; n < totalVertex; n++)
            {
                if (!start.Equals(n + 1))
                {
                    dist[n] = double.MaxValue;
                    prev[n] = 0;
                    final[n] = false;
                }
            }
            adjLine = VertexTable.Rows[start - 1]["line_adj_id"].ToString();
            tmpAdjLine = adjLine.Split('_');
            //����start�����ӵıߵ�distֵ
            for (int m = 0; m < tmpAdjLine.Length; m++)
            {
                tmpLine = tmpAdjLine[m];
                sqlstr = "select source,target,length from " + ftablename + " where gid=" + tmpLine;
                tmpLengthTable = postDB.DoQueryEx(sqlstr);
                tmpVertex = tmpLengthTable.Rows[0]["source"].ToString();
                tmpLength = tmpLengthTable.Rows[0]["length"].ToString();
                if (startpoint.Equals(tmpVertex))
                {
                    tmpVertex = tmpLengthTable.Rows[0]["target"].ToString();
                    dist[int.Parse(tmpVertex) - 1] = double.Parse(tmpLength);
                    prev[int.Parse(tmpVertex) - 1] = start - 1;
                }
                else
                {
                    dist[int.Parse(tmpVertex) - 1] = double.Parse(tmpLength);
                    prev[int.Parse(tmpVertex) - 1] = start - 1;
                }
            }
            dist[start - 1] = 0;
            final[start - 1] = true;

            //���end��㱻��Ǽ������Ѿ��ҵ�����֮������·��
            while (!final[end - 1])
            {
                curVertex = start;
                minLength = double.MaxValue;
                for (int i = 0; i < totalVertex; i++)
                {
                    if (!final[i] && dist[i] < minLength)
                    {
                        //�˴���õ�curVertex����ʵ�Ľ��idС1
                        curVertex = i + 1;
                        minLength = dist[i];
                    }
                }
                final[curVertex - 1] = true;
                #region �ڽӱ�
                //����ͨ�����������ڴ��еı�ļ�¼��ȡ�����ڽӱ����
                adjLine = VertexTable.Rows[curVertex - 1]["line_adj_id"].ToString();
                tmpAdjLine = adjLine.Split('_');
                double tmpdist;
                for (int j = 0; j < tmpAdjLine.Length; j++)
                {
                    tmpLine = tmpAdjLine[j];
                    sqlstr = "select source,target,length from " + ftablename + " where gid=" + tmpLine;
                    tmpLengthTable = postDB.DoQueryEx(sqlstr);
                    tmpVertex = tmpLengthTable.Rows[0]["source"].ToString();
                    tmpLength = tmpLengthTable.Rows[0]["length"].ToString();
                    if (curVertex.Equals(int.Parse(tmpVertex)))
                    {
                        tmpVertex = tmpLengthTable.Rows[0]["target"].ToString();
                    }
                    //��ǰ���ӵĽ��δ�����
                    if (!final[int.Parse(tmpVertex) - 1])
                    {
                        tmpdist = dist[curVertex - 1] + double.Parse(tmpLength);
                        if (tmpdist < dist[int.Parse(tmpVertex) - 1])
                        {
                            dist[int.Parse(tmpVertex) - 1] = tmpdist;
                            //�˴����Ը���Ϊ����߶ε����ƺͶ�Ӧ�ĳ��ȣ�����ڽ��������ʾ
                            prev[int.Parse(tmpVertex) - 1] = curVertex - 1;
                        }
                    }
                }
                #endregion

            }

            List<int> result;
            int tmp;
            string tmpGeomTxt;

            result = new List<int>();
            result.Add(end);
            tmp = prev[end - 1] + 1;
            while (!tmp.Equals(start))
            {
                result.Add(tmp);
                tmp = prev[tmp - 1] + 1;
            }
            result.Add(start);
            result.Reverse();
            //�����д�뵽����
            //д������ݰ�����gid,geom,length
            //�������������checkcreatetable
            tablename = GetLayerName(ftablename) + "_classicaldijkstra";
            sqlstr = "create table " + tablename + " (gid integer,length double precision,geom geometry)";
            postDB.CheckCreateTable(tablename, sqlstr);
            for (int i = 0; i < result.Count - 1; i++)
            {
                int cur, next;
                cur = result[i];
                next = result[i + 1];
                tmpLine = GetLineID(ftablename, cur.ToString(), next.ToString());
                sqlstr = "select length,st_astext(geom) as geomtxt from " + ftablename + " " + "where gid=" + tmpLine;
                tmpLengthTable = postDB.DoQueryEx(sqlstr);
                tmpLength = tmpLengthTable.Rows[0]["length"].ToString();
                tmpGeomTxt = tmpLengthTable.Rows[0]["geomtxt"].ToString();
                sqlstr = "INSERT INTO" + " " + tablename + " " + "(gid,length,geom) values (" + tmpLine + ","
                             + tmpLength + "," + "st_geomfromtext('" + tmpGeomTxt + "', 4326)" + ")";
                postDB.ExecNonQuery(sqlstr);
            } 
        }
#endregion

        /// <summary>
        /// �����������id�ҵ���Ӧ�ߵ�id
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        private string GetLineID(string ftablename,string point1, string point2)
        {
            DataTable dt;
            string sqlstr,line;

            sqlstr = "select gid from " + ftablename + " where source=" + point1 + " and target=" + point2 + " or source=" + point2 + " and target=" + point1;
            dt = postDB.DoQueryEx(sqlstr);
            line = dt.Rows[0]["gid"].ToString();
            return line;
        }

        
        public void Astar(string ftablename, string startpoint, string endpoint)
        {
#region ʹ���ֵ䷽ʽ
            //����ʵ�ַ�ʽ��һ��ʹ���ֶδ洢��ͬ��ֵ������һ��ʹ�ù����Point��ʵ��
            //ʹ���ֶη�ʽ���
            Queue<int> Open = new Queue<int>();
            //Queue<int> Open = new Queue<int>();
            Queue<int> Close = new Queue<int>();
            //��ǰ���·��ֵ
            Hashtable GScore = new Hashtable();
            //��ǰ��㵽��ֹ���֮��Ĺ���ֵ
            Hashtable HScore = new Hashtable();
            //�ܺ�=��ǰ���+����ֵ
            Hashtable FScore = new Hashtable();
            //���ﵱǰ����ǰһ�����·���ϵĽ��
            Hashtable ComeFrom = new Hashtable();

            string sqlstr, tablename, tmpAdjVertex;
            DataTable VertexTable;
            int totalVertex, start, end, curVertex, adjVertex,tmpVertexId;
            string tmpVertex;
            string[] adjVertexes;
            

            tablename = GetLayerName(ftablename) + "_vertex_adjaction";
            sqlstr = "select vertex_id,vertex_adj_id,line_adj_id from " + tablename + " " + "order by vertex_id asc";
            VertexTable = postDB.DoQueryEx(sqlstr);
            totalVertex = VertexTable.Rows.Count;

            ////��ʼ��
            start = int.Parse(startpoint);
            end = int.Parse(endpoint);
            //��ʼ�����뵽open������
            Open.Enqueue(start);
            curVertex = start;
            for (int i = 0; i < totalVertex;i++ )
            {
                tmpVertex = VertexTable.Rows[i]["vertex_id"].ToString();
                tmpVertexId = Convert.ToInt32(tmpVertex);
                GScore.Add(tmpVertexId, MaxDistance);
                HScore.Add(tmpVertexId, 0.0);
                FScore.Add(tmpVertexId, MaxDistance);
            }
            GScore[curVertex] = (double)0.0;
            HScore[curVertex] = (double)GetHeuristic(curVertex, end, ftablename);
            FScore[curVertex] = (double)HScore[curVertex];
            while (Open.Count > 0)
            {
                //��open����ȡ��FScore��С�Ľ��
                //open������ֻ�洢�н����
                curVertex = GetMinTotalCostVertex(Open, FScore);
                
                if (curVertex.Equals(end))
                {
                    //�������������·��д������
                    ConstructPath(ComeFrom,start,end,ftablename);
                    return;
                }
                //��open������ɾ����Сֵ��Ӧ�Ľ�㣬����dequeue����
                Open=RemoveMinFromOpen(Open, curVertex);
                Close.Enqueue(curVertex);

                tmpAdjVertex = VertexTable.Rows[curVertex - 1]["vertex_adj_id"].ToString();
                adjVertexes = tmpAdjVertex.Split('_');

                bool newIsBetter = false;
                double tentativeGScore;
                for (int i = 0; i < adjVertexes.Length; i++)
                {
                    adjVertex = int.Parse(adjVertexes[i]);
                    //��������close���У��������ٽ��в�����������һ��
                    if (! Close.Contains(adjVertex))
                    {
                        tentativeGScore = (double)GScore[curVertex] + GetDistance(curVertex, adjVertex, ftablename);
                        //�������open������
                        if (!Open.Contains(adjVertex))
                        {
                            Open.Enqueue(adjVertex);
                            newIsBetter = true;
                        }
                        else if (tentativeGScore < (double) GScore[adjVertex])
                        {
                            newIsBetter = true;
                        }

                        if (newIsBetter)
                        {
                            ComeFrom[adjVertex] = curVertex;
                            GScore[adjVertex] = tentativeGScore;
                            HScore[adjVertex] = GetHeuristic(adjVertex, end, ftablename);
                            FScore[adjVertex] = (double)GScore[adjVertex] + (double) HScore[adjVertex];
                        }
                    }
                    
                }

            }
            //�����õ�comefromΪ�գ���δ�ҵ���Ӧ��·��
            //����������δ�õ����˵��δ�ҵ���ص�·��
#endregion
            
        }

        /// <summary>
        /// �Ӷ�����ɾ��ָ���Ľ��
        /// </summary>
        /// <param name="Open">����</param>
        /// <param name="curVertex">��Ҫɾ���Ľ��</param>
        private Queue<int> RemoveMinFromOpen(Queue<int> Open, int curVertex)
        {
            if (Open.Count <1)
            {
                return null;
            }
            //ʹ�������һ������Ϊ������������һ�γ��У�����Ŀ���㣬��󽫸������и�ֵ��ԭ����
            Queue<int> tmpOpen = new Queue<int>();
            int tmpVertex;
            while (Open.Count>0)
            {
                tmpVertex = Open.Dequeue();
                if (! tmpVertex.Equals(curVertex))
                {
                    tmpOpen.Enqueue(tmpVertex);
                }
            }
            return tmpOpen;
        }


        /// <summary>
        /// ������Ŀ�����·������д�����ݿ��
        /// </summary>
        /// <param name="ComeFrom">parent����</param>
        /// <param name="end">Ŀ����</param>
        /// <param name="ftablename">Ҫ�����ı�</param>
        private void ConstructPath(Hashtable ComeFrom, int start,int end,string ftablename)
        {
            List<int> result;
            int tmpid;
            string tmpGeomTxt, tmpLine, tmpLength;
            string tablename, sqlstr;
            DataTable tmpLengthTable;

            result = new List<int>();
            //���Ŀ����
            result.Add(end);
            //����׷�ٵ���ʼ��
            tmpid =(int) ComeFrom[end];
            while (!tmpid.Equals(start))
            {
                result.Add(tmpid);
                tmpid = (int)ComeFrom[tmpid];
            }
            result.Add(start);
            result.Reverse();
            //�����д�뵽����
            //д������ݰ�����gid,geom,length
            //�������������checkcreatetable
            tablename = GetLayerName(ftablename) + "_classicalastar";
            sqlstr = "create table " + tablename + " (gid integer,length double precision,geom geometry)";
            postDB.CheckCreateTable(tablename, sqlstr);
            for (int i = 0; i < result.Count - 1; i++)
            {
                int cur, next;
                cur = result[i];
                next = result[i + 1];
                tmpLine = GetLineID(ftablename, cur.ToString(), next.ToString());
                sqlstr = "select length,st_astext(geom) as geomtxt from " + ftablename + " " + "where gid=" + tmpLine;
                tmpLengthTable = postDB.DoQueryEx(sqlstr);
                tmpLength = tmpLengthTable.Rows[0]["length"].ToString();
                tmpGeomTxt = tmpLengthTable.Rows[0]["geomtxt"].ToString();
                sqlstr = "INSERT INTO" + " " + tablename + " " + "(gid,length,geom) values (" + tmpLine + ","
                             + tmpLength + "," + "st_geomfromtext('" + tmpGeomTxt + "', 4326)" + ")";
                postDB.ExecNonQuery(sqlstr);
            }
        }

        /// <summary>
        /// ����open���ҳ���Ӧ��cost������cost��С�����
        /// </summary>
        /// <param name="Open">��ŵ�ǰ�ѷ��ʵĽ��</param>
        /// <param name="FScore">���н��ĺķ�</param>
        /// <returns>�ķ���С�Ľ����</returns>
        private int GetMinTotalCostVertex(Queue<int> Open, Hashtable FScore)
        {
            double minCost =MaxDistance;
            double tmpCost = 0.0;
            int minVertex=-1;

            foreach (int vertex in Open)
            {
                tmpCost =(double)FScore[vertex];
                if (minCost>=tmpCost)
                {
                    minCost = tmpCost;
                    minVertex = vertex;
                }
            }
            return minVertex;
        }

        //��ȡ��ǰ������ڽӵĽ��֮��ߵ�Ȩֵ
        private double GetDistance(int curVertex, int adjVertex, string ftablename)
        {
            double length;
            string sqlstr;
            DataTable tmpdt;

            sqlstr = "select length from " + " " + ftablename + " " + "where source=" + curVertex.ToString() + " and target=" + adjVertex.ToString() + " or source=" + adjVertex.ToString() + " and target=" + curVertex.ToString();
            tmpdt = postDB.DoQueryEx(sqlstr);
            length = double.Parse(tmpdt.Rows[0]["length"].ToString());

            return length;

        }

        //��ȡ��ǰ�������·��ֵ
        private double GetCost(int curVertex, Dictionary<int, double> GScore)
        {
            double cost;

            if (GScore.ContainsKey(curVertex))
            {
                cost = GScore[curVertex];
            } 
            else
            {
                cost=999999;
            }
            return cost;
        }
#region A*�㷨
        //���㵱ǰ��㵽��ֹ��������ֵ
        private double GetHeuristic(int curVertex, int end, string ftablename)
        {
            string sqlstr;
            double dist,x1,y1,x2,y2;
            DataTable tmpdt;
            string tablename;

            tablename = GetLayerName(ftablename) + "_vertices";
            sqlstr = "select st_x(st_transform(the_geom,2345)) as x,st_y(st_transform(the_geom,2345)) as y from " + tablename + " " + "where id=" + curVertex.ToString();
            tmpdt = postDB.DoQueryEx(sqlstr);
            x1 = double.Parse(tmpdt.Rows[0]["x"].ToString());
            y1 = double.Parse(tmpdt.Rows[0]["y"].ToString());
            sqlstr = "select st_x(st_transform(the_geom,2345)) as x,st_y(st_transform(the_geom,2345)) as y from " + tablename + " " + "where id=" + end.ToString();
            tmpdt = postDB.DoQueryEx(sqlstr);
            x2 = double.Parse(tmpdt.Rows[0]["x"].ToString());
            y2 = double.Parse(tmpdt.Rows[0]["y"].ToString());
            dist = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            return dist;
        }

#endregion
        

        
        private string GetLayerName(string ftablename)
        {
            int index;
            string name;
            index = ftablename.LastIndexOf('_');
            name = ftablename.Substring(0, index);
            return name;
        }


    }
}
