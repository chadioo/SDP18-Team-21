using System.Collections;
using System.Threading;
using UnityEngine;

namespace HoV {
	public enum UnityBackgroundWorkerStatus {
		Idle,
		Busy,
		Done,
		Aborted,
		HasError
	}
	
	public class UnityBackgroundWorkerArguments {
		public int Progress;
		public bool HasError;
		public string ErrorMessage;
	}
	
	public class UnityBackgroundWorkerInformation {
		public int Progress;
		public UnityBackgroundWorkerStatus Status;
		public string ErrorMessage;
	}
	
	public delegate void DoWork(object CustomData, UnityBackgroundWorkerArguments e);
	public delegate void ProgressReport(object CustomData, int Progress);
	public delegate void WorkDone(object CustomData, UnityBackgroundWorkerInformation Information);

	public class UnityBackgroundWorker {
		MonoBehaviour Caller;
		Thread WorkerThread;
		
		object Data;
		public bool IsBusy;
		UnityBackgroundWorkerInformation Information;
		DoWork WorkMethod;
		ProgressReport ProgressMethod;
		WorkDone DoneMethod;
		
		public UnityBackgroundWorker(MonoBehaviour caller, DoWork DoWorkMethod, ProgressReport ProgressReportMethod, WorkDone WorkDoneMethod, object CustomData) {
			Caller = caller;
			Data = CustomData;
			Information = new UnityBackgroundWorkerInformation();
			WorkMethod = DoWorkMethod;
			ProgressMethod = ProgressReportMethod;
			DoneMethod = WorkDoneMethod;
		}
		
		public void Run () {
			if (!IsBusy) Caller.StartCoroutine(RunRoutine());
		}
		
		public void Abort() {
			if (WorkerThread.IsAlive) {
				Information.Status = UnityBackgroundWorkerStatus.Aborted;
				WorkerThread.Abort();
			}
		}
		
		public IEnumerator RunRoutine () {
			IsBusy = true;
			Information.Status = UnityBackgroundWorkerStatus.Busy;
			UnityBackgroundWorkerArguments args = new UnityBackgroundWorkerArguments();
			WorkerThread = new Thread(() => WorkMethod(Data,args));
			WorkerThread.IsBackground = true;
			WorkerThread.Start();
			while (WorkerThread.IsAlive) {
				yield return null;
				if (Information.Progress != args.Progress) {
					Information.Progress = args.Progress;
					ProgressMethod(Data, Information.Progress);
				}
			}
			if (args.HasError) {
				Information.ErrorMessage = args.ErrorMessage;
				Information.Status = UnityBackgroundWorkerStatus.HasError;
			}
			if (Information.Status == UnityBackgroundWorkerStatus.Busy)
				Information.Status = UnityBackgroundWorkerStatus.Done;
			DoneMethod(Data, Information);
			args = null;
			WorkerThread = null;
			Information = null;
			Information = new UnityBackgroundWorkerInformation();
			IsBusy = false;
		}
	}
}
