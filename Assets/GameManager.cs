/*
 * 
 * Lectura de tarjetas NFC con arduino para mostar un objeto voxeloso
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO.Ports;

public class GameManager : MonoBehaviour
{
	//prefab de cubo o voxel
	public Transform voxelPrefab;

	public Text labelInfo;

	public Transform panelError;

	SerialPort stream = new SerialPort("COM3", 115200);

	//referencias a los cubos, debe ser de 4096
	public Transform[] voxels;

	public enum VoxelPalette
    {
        empty, // cubo vacio
        red,
        green,
        blue,
        white,
        black
    }

	// Use this for initialization
	void Start ()
	{
		//inicializar el arreglo de voxels
		voxels = new Transform[4096];

		for (int i = 0; i < 16; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				for (int k = 0; k < 16; k++)
				{
					Transform voxel = Instantiate(voxelPrefab) as Transform;
					voxel.position = new Vector3(i, j, k);


					Material mat = voxel.GetComponent<MeshRenderer>().material;
					mat.color = GetColorPalette(VoxelPalette.empty);
					//mat.color = GetColorPalette((VoxelPalette)Random.RandomRange(0, 6));

					int idx1d = (16 * 16 * k) + (j * 16 + i);
					//Debug.Log ("indice 3d: " + i+","+j+","+k+" - indice 1d: " + idx1d);
					voxel.name = "voxel " + idx1d;
					voxels[idx1d] = voxel;
				}
			}
		}



			try
		{
			stream.Open();
			stream.ReadTimeout = 1;
		}
		catch(System.Exception e)
		{
			Debug.Log("Error en open: " + e.Message);

            //mostrar ventana de error
            panelError.gameObject.SetActive(true);
            panelError.Find("Text").GetComponent<Text>().text = "ERROR no se ha detectado el hardware. \n" + e.Message;

        }
		labelInfo.text = "inicialziando...";

		//Leer desde tezto las propiedades de los voxels
		//ReadPropertiesfromFile();
	}

	Transform GetVoxel(int x, int y, int z)
	{
		if (x > 16 || y > 16 || z > 16)
		{
			Debug.LogWarning("indice incorrecto, mayor a 16");
			return null;
		}
		else
		{
			return voxels[(16 * 16 * z) + (y * 16 + x)];
		}
	}

	void SetVoxel(Transform v, int x, int y, int z)
	{
		if (x > 16 || y > 16 || z > 16)
		{
			Debug.LogWarning("indice incorrecto, mayor a 16");
			return;
		}
		voxels[(16 * 16 * z) + (y * 16 + x)] = v;
	}

	//obtiene un color real de la paleta
	public Color GetColorPalette(VoxelPalette colorPalette)
    {
        switch(colorPalette)
        {
            case VoxelPalette.empty:
                return new Color(0, 0, 0, 0);
                break;
            case VoxelPalette.red:
                return new Color(1, 0, 0, 1);
                break;
            case VoxelPalette.green:
                return new Color(0, 1, 0, 1);
                break;
            case VoxelPalette.blue:
                return new Color(0, 0, 1,1);
                break;
            case VoxelPalette.white:
                return new Color(1, 1, 1, 1);
                break;
            case VoxelPalette.black:
                return new Color(0, 0, 0, 1);
                break;
            default:
                return new Color(1, 1, 1, 1);
                break;
        }
    }

	public void ReadPropertiesfromFile()
	{

		/*  modificando harcoded 
		for(int i = 0; i< 16; i++)
		{
			for(int j = 0; j< 16; j++)
			{
				Transform vox = GetVoxel(i,0,j);
				vox.GetComponent<MeshRenderer>().material.color = GetColorPalette( VoxelPalette.black) ;
				SetVoxel( vox, i, 0 ,j );

				vox = GetVoxel(i,1,j);
				vox.GetComponent<MeshRenderer>().material.color = GetColorPalette( VoxelPalette.green) ;
				SetVoxel( vox, i, 1 ,j );


				GetVoxel(8,2,8).GetComponent<MeshRenderer>().material.color = GetColorPalette( VoxelPalette.red) ;

				
			}
		}

		*/

		TextAsset texto = Resources.Load<TextAsset>("mapa");
		string[] lineas = texto.text.Split('\n');
		for (int i = 0; i < lineas.Length; i++)
		{
			//dicidir cada linea e interpretar la coordenada
			string[] coord = lineas[i].Split(',');
			int x = System.Int32.Parse(coord[0]);
			int y = System.Int32.Parse(coord[1]);
			int z = System.Int32.Parse(coord[2]);
			VoxelPalette vcolor = (VoxelPalette)System.Int32.Parse(coord[3]);

			GetVoxel(x, y, z).GetComponent<MeshRenderer>().material.color = GetColorPalette(vcolor);
		}
	}

    public void ReadPropertiesFromPort()
    {
        //lectura de puerto serie
        try
        {
            string value = stream.ReadLine();

            //labelInfo.text = "Recibido : " + value;
            Debug.Log("Recibido : " + value);
            //analizar el contenido del mensaje
            string[] s = value.Split(' ');
            if (s[0] == "Error" && s[1] == "20")
            {
                panelError.gameObject.SetActive(true);
                panelError.Find("Text").GetComponent<Text>().text = "ERROR tarjeta desconocida";
            }
            else
            {
                //tarjeta correcta, leer los bytes
                panelError.gameObject.SetActive(false);
                if (s.Length < 16)
                {
                    labelInfo.text = "error, menos de 16 bytes leidos";
                }
                else
                {
                    ushort maskX = 0xF0;
                    ushort maskY = 0x0F;
                    for( int i = 0; i< s.Length; i+=2)
                    {
                        //coordenada en X
                        //parsear el string y convertirlo a WORD 
                        char coordx = (char) System.Convert.ToUInt16(s[i], 16);
                        //Debug.Log( (int)coordx);
                        coordx = (char) (coordx & maskX);
                        //Debug.Log((int) coordx);
                        coordx = (char) (coordx >> 4) ;

                        //coordenada en Y
                        char coordy = (char)System.Convert.ToUInt16(s[i], 16);
                        coordy = (char)(coordy & maskY);

                        //coordenada en Z
                        char coordz = (char)System.Convert.ToUInt16(s[i+1], 16);
                        //Debug.Log( (int)coordx);
                        coordz = (char)(coordz & maskX);
                        //Debug.Log((int) coordx);
                        coordz = (char)(coordz >> 4);

                        //coordenada del color
                        char coordcolor = (char)System.Convert.ToUInt16(s[i+1], 16);
                        coordcolor = (char)(coordcolor & maskY);




                        Debug.Log("Leido voxel : (" + (int)coordx + "," + (int)coordy +"," + (int)coordz +"," + GetColorPalette( (VoxelPalette)coordcolor)  );

                        //Aplicar esa info a un voxel 

                        Transform vox = GetVoxel((int)coordx, (int)coordy, (int)coordz);
                        vox.GetComponent<MeshRenderer>().material.color = GetColorPalette((VoxelPalette)coordcolor);
                        SetVoxel(vox, (int)coordx, (int)coordy, (int)coordz);

                    }
                }
            }
        }
        catch (System.Exception e)
        {
            //labelInfo.text = e.Message;
        }
    }

    public void Update()
    {
        if (stream.IsOpen)
        {
            ReadPropertiesFromPort();
        }
		
	}
}
