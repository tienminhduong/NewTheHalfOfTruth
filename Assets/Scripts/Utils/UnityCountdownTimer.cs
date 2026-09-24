using System;

public class UnityCountdownTimer
{
    private float _timeLeft;
    private float _maxTime;
    
    private Action _onTimerComplete;

    public bool IsRunning { get; set; } = false;
    public bool IsLooped { get; set; } = false;

    public UnityCountdownTimer(bool isLooped = false)
    {
        IsLooped = isLooped;
    }

    public void UpdateTimer(float deltaTime)
    {
        if (IsRunning)
        {
            _timeLeft -= deltaTime;
            if (_timeLeft <= 0)
            {
                IsRunning = false;
                _onTimerComplete?.Invoke();

                if (IsLooped)
                {
                    _timeLeft = _maxTime;
                    IsRunning = true;
                }
            }
        }
    }

    public void StartTimer(float duration)
    {
        _maxTime = duration;
        _timeLeft = duration;
        IsRunning = true;
    }

    public void StopTimer(bool isTriggerComplete = false)
    {
        IsRunning = false;
        _timeLeft = 0;
        if (isTriggerComplete)
        {
            _onTimerComplete?.Invoke();
        }
    }

    public void ResetTimer()
    {
        _timeLeft = _maxTime;
        IsRunning = false;
    }

    public float TimeLeft => _timeLeft;
    public float MaxTime => _maxTime;
    public float TimeElapsed => _maxTime - _timeLeft;
    public float TimeElapsedPercentage => _maxTime > 0 ? (_maxTime - _timeLeft) / _maxTime : 0;
    public float TimeLeftPercentage => _maxTime > 0 ? _timeLeft / _maxTime : 0;

    public void SetOnTimerComplete(Action onTimerComplete)
    {
        _onTimerComplete = onTimerComplete;
    }

    public void RemoveOnTimerComplete()
    {
        _onTimerComplete = null;
    }

    public void Destroy()
    {
        _onTimerComplete = null;
    }
}