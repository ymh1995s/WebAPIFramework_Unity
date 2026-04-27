using UnityEngine;

public static class Define
{
    public enum EScene
    {
        Unknown,
        LoginScene,
        MainScene,
        GameScene,
    }

    public enum EEventType
    {
        None,
        GoldChanged,
        LanguageChanged,
        LoginSuccess,   // 로그인 성공 시 발행
    }

    public enum ESound
    {
        Bgm,
        Effect,

        MaxCount
    }

    public enum ELanguage
    {
        KOR,
        ENG,
    }

	public enum EAnimation
	{
		b_wait,
		b_walk,
		f_wait,
		f_walk
	}

	public enum ECatState
	{
		Idle,
		Move,
		Work
	}
}
