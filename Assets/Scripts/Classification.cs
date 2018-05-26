using UnityEngine;
using TensorFlow;
using System.Linq;

public class Classification : MonoBehaviour {

    [Header("Constants")]
	private const int INPUT_SIZE = 224;
	private const int IMAGE_MEAN = 117;
	private const float IMAGE_STD = 1;
	private const string INPUT_TENSOR = "input";
	private const string OUTPUT_TENSOR = "output";

    [Header("Inspector Stuff")]
	public CameraFeedBehavior camFeed;
    public TextAsset labelMap;
    public TextAsset model;
	public AudioSource audioSource;

	private TFGraph graph;
	private TFSession session;
	private string [] labels;

	// Use this for initialization
	void Start() {
#if UNITY_ANDROID
        TensorFlowSharp.Android.NativeBinding.Init();
#endif
		//load labels into string array
		labels = labelMap.ToString ().Split ('\n');
		//load graph
		graph = new TFGraph ();
		graph.Import (model.bytes);
		session = new TFSession (graph);
    }

	private void Update () {
		//process image on click or touch
		if (Input.GetMouseButtonDown(0)){
			ProcessImage ();
		}
	}

	void ProcessImage(){
		//pass in input tensor
		var tensor = TransformInput (camFeed.GetImage (), INPUT_SIZE, INPUT_SIZE);
        var runner = session.GetRunner();
		runner.AddInput (graph [INPUT_TENSOR] [0], tensor).Fetch (graph [OUTPUT_TENSOR] [0]);
        var output = runner.Run();
		//put results into one dimensional array
		float[] probs = ((float [] [])output[0].GetValue (jagged: true)) [0];
		//get max value of probabilities and find its associated label index
		float maxValue = probs.Max ();
		int maxIndex = probs.ToList ().IndexOf (maxValue);
		//print label with highest probability
		string label = labels [maxIndex];
		print (label);

		audioSource.clip = Resources.Load ("imageNetSounds/" + label) as AudioClip;
		audioSource.Play ();
	}

	//stole from https://github.com/Syn-McJ/TFClassify-Unity
	public static TFTensor TransformInput (Color32 [] pic, int width, int height) {
		float [] floatValues = new float [width * height * 3];

		for (int i = 0; i < pic.Length; ++i) {
			var color = pic [i];

			floatValues [i * 3 + 0] = (color.r - IMAGE_MEAN) / IMAGE_STD;
			floatValues [i * 3 + 1] = (color.g - IMAGE_MEAN) / IMAGE_STD;
			floatValues [i * 3 + 2] = (color.b - IMAGE_MEAN) / IMAGE_STD;
		}

		TFShape shape = new TFShape (1, width, height, 3);

		return TFTensor.FromBuffer (shape, floatValues, 0, floatValues.Length);
	}
}

