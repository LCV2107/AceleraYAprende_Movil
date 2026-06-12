using UnityEngine;
using System;

public class CoinEffect : MonoBehaviour
{
    public float velocidadRotacion = 180f; // grados/seg
    public float velocidadMovimiento = 5f; // unidades/seg
    public float alturaInicial = -5f;
    public float alturaMaxima = 8f;
    public float alturaCentro = 0f;

    [Tooltip("Margen en grados para considerar rotación Y como 0 y detener")]
    public float margenRotacionY = 5f;

    private enum EstadoMovimiento
    {
        Subiendo,
        Cayendo,
        EsperandoRotacionCero,
        Detenido
    }

    private EstadoMovimiento estado = EstadoMovimiento.Subiendo;

    public event Action OnAnimacionTerminada;

    void Start()
    {
        Vector3 pos = transform.position;
        pos.y = alturaInicial;
        transform.position = pos;
    }

    void Update()
    {
        Vector3 pos = transform.position;

        switch (estado)
        {
            case EstadoMovimiento.Subiendo:
                pos.y += velocidadMovimiento * Time.deltaTime;
                if (pos.y >= alturaMaxima)
                {
                    pos.y = alturaMaxima;
                    estado = EstadoMovimiento.Cayendo;
                }
                break;

            case EstadoMovimiento.Cayendo:
                pos.y -= velocidadMovimiento * Time.deltaTime;
                if (pos.y <= alturaCentro)
                {
                    pos.y = alturaCentro;
                    estado = EstadoMovimiento.EsperandoRotacionCero;
                }
                break;

            case EstadoMovimiento.EsperandoRotacionCero:
                // Seguir girando a velocidad constante
                transform.Rotate(0, velocidadRotacion * Time.deltaTime, 0);

                // Obtener rotación Y actual en rango [0,360)
                float rotY = transform.eulerAngles.y;

                // Comprobar si rotación Y está cerca de 0 (o 360)
                if (rotY <= margenRotacionY || rotY >= 360f - margenRotacionY)
                {
                    // Fijar rotación exacta a 0 para evitar errores visuales
                    transform.eulerAngles = Vector3.zero;

                    estado = EstadoMovimiento.Detenido;
                    OnAnimacionTerminada?.Invoke();
                }
                break;

            case EstadoMovimiento.Detenido:
                // No gira ni se mueve
                break;
        }

        // Girar durante subir y bajar
        if (estado == EstadoMovimiento.Subiendo || estado == EstadoMovimiento.Cayendo)
        {
            transform.Rotate(0, velocidadRotacion * Time.deltaTime, 0);
        }

        transform.position = pos;
    }

    public void ReiniciarAnimacion()
    {
        estado = EstadoMovimiento.Subiendo;
        Vector3 pos = transform.position;
        pos.y = alturaInicial;
        transform.position = pos;
    }
}