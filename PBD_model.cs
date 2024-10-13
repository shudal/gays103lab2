using UnityEngine;
using System.Collections;

public class PBD_model: MonoBehaviour {

	float 		t= 0.0333f;
	float		damping= 0.99f;
	// edge。每两个元素表示一条edge。
	int[] 		E;
	// edge 原长。长度为 E.Lenght / 2
	float[] 	L;
	// 每个vertex的速度
	Vector3[] 	V;

	bool IsVertexIdxIgnore(int x)
	{
		bool Res = false;
		if (x == 0 || x == 20)
		{
			Res = true;
		}
		return Res;
	}
	// Use this for initialization
	void Start () 
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;

		//Resize the mesh.
		int n=21;
		Vector3[] X  	= new Vector3[n*n];
		Vector2[] UV 	= new Vector2[n*n];
		
		// 三角形数量。把四边形像棋盘一样划分，每个棋盘格子再中心切一刀。
		// 棋盘格子数(n-1)*(n-1)；因为格子切一刀，所以x2；每个三角形有三个顶点，所以x3
		int[] T	= new int[(n-1)*(n-1)*6];

		// 以原点为中心,(5,5)为第0个顶点位置，第0个顶点的uv为(0,0)。
		// 位置范围为x、y轴的[-5,5]范围
		for(int j=0; j<n; j++)
		for(int i=0; i<n; i++)
		{
			X[j*n+i] =new Vector3(5-10.0f*i/(n-1), 0, 5-10.0f*j/(n-1));
			UV[j*n+i]=new Vector3(i/(n-1.0f), j/(n-1.0f));
		}
		int t=0;
		for(int j=0; j<n-1; j++)
		for(int i=0; i<n-1; i++)	
		{
			// 添加每个棋盘格子的两个三角形
			T[t*6+0]=j*n+i;
			T[t*6+1]=j*n+i+1;
			T[t*6+2]=(j+1)*n+i+1;
			T[t*6+3]=j*n+i;
			T[t*6+4]=(j+1)*n+i+1;
			T[t*6+5]=(j+1)*n+i;
			t++;
		}
		mesh.vertices	= X;
		mesh.triangles	= T;
		mesh.uv 		= UV;
		mesh.RecalculateNormals ();

		//Construct the original edge list
		// _E里面有重复的edge，后面去重后得到E[]
		int[] _E = new int[T.Length*2];
		for (int i=0; i<T.Length; i+=3) 
		{
			_E[i*2+0]=T[i+0];
			_E[i*2+1]=T[i+1];
			_E[i*2+2]=T[i+1];
			_E[i*2+3]=T[i+2];
			_E[i*2+4]=T[i+2];
			_E[i*2+5]=T[i+0];
		}
		//Reorder the original edge list
		for (int i=0; i<_E.Length; i+=2)
			if(_E[i] > _E[i + 1]) 
				Swap(ref _E[i], ref _E[i+1]);
		//Sort the original edge list using quicksort
		Quick_Sort (ref _E, 0, _E.Length/2-1);

		int e_number = 0; // 去重后的edge的数量
		for (int i=0; i<_E.Length; i+=2)
			if (i == 0 || _E [i + 0] != _E [i - 2] || _E [i + 1] != _E [i - 1]) // i==0 或 两个edge的第1/2个顶点不一样
				e_number++;

		E = new int[e_number * 2];
		for (int i=0, e=0; i<_E.Length; i+=2)
			if (i == 0 || _E [i + 0] != _E [i - 2] || _E [i + 1] != _E [i - 1]) 
			{
				E[e*2+0]=_E [i + 0];
				E[e*2+1]=_E [i + 1];
				e++;
			}

		L = new float[E.Length/2];
		for (int e=0; e<E.Length/2; e++) 
		{
			int i = E[e*2+0];
			int j = E[e*2+1];
			L[e]=(X[i]-X[j]).magnitude;
		}

		V = new Vector3[X.Length];
		for (int i=0; i<X.Length; i++)
			V[i] = new Vector3 (0, 0, 0);
	}

	void Quick_Sort(ref int[] a, int l, int r)
	{
		int j;
		if(l<r)
		{
			j=Quick_Sort_Partition(ref a, l, r);
			Quick_Sort (ref a, l, j-1);
			Quick_Sort (ref a, j+1, r);
		}
	}

	int  Quick_Sort_Partition(ref int[] a, int l, int r)
	{
		int pivot_0, pivot_1, i, j;
		pivot_0 = a [l * 2 + 0];
		pivot_1 = a [l * 2 + 1];
		i = l;
		j = r + 1;
		while (true) 
		{
			do ++i; while( i<=r && (a[i*2]<pivot_0 || a[i*2]==pivot_0 && a[i*2+1]<=pivot_1));
			do --j; while(  a[j*2]>pivot_0 || a[j*2]==pivot_0 && a[j*2+1]> pivot_1);
			if(i>=j)	break;
			Swap(ref a[i*2], ref a[j*2]);
			Swap(ref a[i*2+1], ref a[j*2+1]);
		}
		Swap (ref a [l * 2 + 0], ref a [j * 2 + 0]);
		Swap (ref a [l * 2 + 1], ref a [j * 2 + 1]);
		return j;
	}

	void Swap(ref int a, ref int b)
	{
		int temp = a;
		a = b;
		b = temp;
	}

	void Strain_Limiting()
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		Vector3[] vertices = mesh.vertices;

		//Apply PBD here.
		//...

		Vector3[] sum_x = new Vector3[vertices.Length];
		for(int i=0;i<sum_x.Length;i++) { sum_x[i] = new Vector3(0f, 0f, 0f); }
		int[] sum_n = new int[vertices.Length];
		for(int i=0;i<sum_n.Length;i++) { sum_n[i] = 0; }
		for (int edgei = 0; edgei < E.Length; edgei += 2)
		{
			int i = E[edgei], j = E[edgei + 1];

			Vector3 x_i = vertices[i], x_j = vertices[j];
			Vector3 v_j2i = x_i - x_j;
			float L_e = L[edgei / 2];
            sum_x[i] = sum_x[i] + 0.5f * (x_i + x_j +  Vector3.Normalize(v_j2i) * L_e);
            sum_x[j] = sum_x[j] + 0.5f * (x_i + x_j - Vector3.Normalize(v_j2i) * L_e);

            sum_n[i]++;
			sum_n[j]++;

		}

		for (int i=0; i<vertices.Length; i++)
		{
			if (IsVertexIdxIgnore(i)) continue;
			Vector3 posNew = (vertices[i] * 0.2f + sum_x[i]) / (0.2f + sum_n[i]);
			V[i] = V[i] + (posNew - vertices[i]) / t;
			vertices[i] = posNew;
		}

		mesh.vertices = vertices;
	}

	void Collision_Handling()
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		Vector3[] X = mesh.vertices;

		//For every vertex, detect collision and apply impulse if needed.
		//...

		var sphereobj = GameObject.Find("Sphere");
		Vector3 c = sphereobj.transform.position;
		float r = 2.7f;
		for (int i=0; i<X.Length; i++)
		{
			Vector3 c2i = X[i] - c;
			if (c2i.magnitude <= r)
			{
				Vector3 posNew  = c + r * Vector3.Normalize(c2i);
				V[i] = V[i] + (posNew - X[i]) / t;
				X[i] = posNew;
			}
		}

		mesh.vertices = X;
	}

	// Update is called once per frame
	void Update () 
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		Vector3[] X = mesh.vertices;

		for(int i=0; i<X.Length; i++)
		{
			if(i==0 || i==20)	continue;
			//Initial Setup
			//...
			
			V[i] = V[i] * damping;

			// 外力率先改变了 弹簧系统中vertex 的位置，后面 Strain_Limiting 时根据改变后的位置计算弹簧的影响。
			V[i] = V[i] +  (new Vector3(0, -9.8f, 0)) * t;

			X[i] = X[i] + V[i] * t;
		}
		mesh.vertices = X;

		// 迭代次数越多，
		// 越感觉布料受外力影响更小、受弹簧系统自己的模拟出的数更大、
		// 更像是生锈了的弹簧、更僵硬、更不容易被外力改变
		// 感觉弹簧k更大
		for(int l=0; l<32; l++)
			Strain_Limiting ();

		Collision_Handling ();

		mesh.RecalculateNormals ();

	}


}

