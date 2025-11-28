namespace CargoClash.Utils
{
    /// <summary>
    /// Constantes globais do jogo.
    /// Centraliza valores para fácil balanceamento e manutenção.
    /// </summary>
    public static class GameConstants
    {
        // ===== GAMEPLAY =====
        public const int VICTORY_SCORE = 15;              // Caixas para vitória
        public const float MATCH_DURATION = 300f;         // 5 minutos em segundos
        public const float CARGO_SPAWN_INTERVAL = 5f;     // Intervalo entre spawns
        public const int MAX_CARGO_BOXES = 6;             // Máximo simultâneo

        // ===== PLAYER =====
        public const float PLAYER_MOVE_SPEED = 5f;        // Velocidade base
        public const float PLAYER_BOOST_MULTIPLIER = 1.5f;// 50% mais rápido
        public const float BOOST_DURATION = 3f;           // Duração do boost
        public const float BOOST_COOLDOWN = 6f;           // Cooldown do boost

        // ===== ABILITIES =====
        public const float OIL_TRAP_COOLDOWN = 12f;       // Cooldown armadilha
        public const int MAX_OIL_TRAPS = 2;               // Máximo simultâneo
        public const float OIL_TRAP_DURATION = 10f;       // Duração da armadilha
        public const float OIL_SLOW_DURATION = 2f;        // Duração do slow
        public const float OIL_SLOW_MULTIPLIER = 0.5f;    // 50% mais lento

        // ===== NETWORK =====
        public const float NETWORK_SEND_RATE = 20f;       // Updates por segundo
        public const float LERP_SPEED = 10f;              // Suavização de movimento

        // ===== SCENE NAMES =====
        public const string LOBBY_SCENE = "Lobby";
        public const string GAME_SCENE = "GameArena";

        // ===== PHOTON CUSTOM PROPERTIES KEYS =====
        public const string PROP_PLAYER_SCORE = "Score";
        public const string PROP_GAME_TIMER = "Timer";
        public const string PROP_GAME_STARTED = "Started";
        public const string PROP_WINNER = "Winner";
    }
}