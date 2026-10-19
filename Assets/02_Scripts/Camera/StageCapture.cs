using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 스테이지 영역(바닥 종이)을 한 장 찍어 텍스처로 돌려준다. 클리어 그림을 액자에 걸 때 쓴다.
// 오버레이 UI는 카메라 그림에 들어가지 않아 그림만 찍힌다. 화면 비율과 상관없이 종이가 그림에 꼭 맞도록, 찍는 동안만 카메라 자리와 크기를 바꾼다.
public static class StageCapture
{
    public static Texture2D Capture(Camera camera, Bounds area, int width, int height)
    {
        Transform cameraTransform = camera.transform;
        Vector3 position = cameraTransform.position;
        float orthographicSize = camera.orthographicSize;
        float aspect = (float)width / height;

        cameraTransform.position = new Vector3(area.center.x, area.center.y, position.z);
        camera.orthographicSize = Mathf.Max(area.extents.y, area.extents.x / aspect);
        camera.aspect = aspect;

        RenderTexture target = RenderTexture.GetTemporary(width, height, 24);
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });

        cameraTransform.position = position;
        camera.orthographicSize = orthographicSize;
        camera.ResetAspect();

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = target;

        // 찍은 그림은 파일로 저장하고 바로 지우므로, GPU로 다시 올리는 Apply는 하지 않는다.
        Texture2D painting = new Texture2D(width, height, TextureFormat.RGB24, false);
        painting.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);

        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(target);

        return painting;
    }
}
