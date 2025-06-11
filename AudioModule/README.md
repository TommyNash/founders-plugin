# Аудио Модуль (Audio Module)

Модуль для работы с аудио в Unity WebGL проектах. Поддерживает два провайдера: стандартный Unity AudioSource и JavaScript (Howler.js).

## Доступные методы

### Воспроизведение

```csharp
// Методы являются async из-за асинхронной загрузки звуков
// Вы можете использовать await для ожидания загрузки
await audioSource.Play("soundKey");

// Или игнорировать await, если не важно дождаться загрузки
audioSource.Play("soundKey").ConfigureAwait(false);

// Воспроизведение с начальной позиции
await audioSource.PlayFromTime("soundKey", 10f); // Начать с 10 секунды

// Зацикленное воспроизведение
await audioSource.PlayLooped("soundKey");
await audioSource.PlayLooped("soundKey", dontDestroyOnLoad: true); // Сохранять между сценами

// Воспроизведение с полными параметрами
var parameters = new PlaySoundParameters
{
    loop = true,                // Зацикливание
    dontDestroyOnLoad = true,  // Сохранять между сценами
    startTime = 30f,           // Начальная позиция (в секундах)
    volume = 0.8f,             // Громкость
    pitch = 1.2f,              // Скорость воспроизведения
    priority = 1               // Приоритет
};
await audioSource.PlayWithParameters("soundKey", parameters);

// Воспроизведение поверх текущего звука (только для Unity провайдера)
await audioSource.PlayOneShot("soundKey");
```

### Управление воспроизведением

```csharp
// Остановка
audioSource.Stop();

// Пауза/Возобновление
audioSource.Pause();
audioSource.UnPause();

// Управление громкостью
audioSource.Volume = 0.5f;
audioSource.Mute = true;

// Управление скоростью воспроизведения
audioSource.Pitch = 1.2f;

// Управление позицией воспроизведения
audioSource.Time = 10f;

// Зацикливание
audioSource.Loop = true;
```

### События

```csharp
// Подписка на события
audioSource.OnFinished += () => Debug.Log("Звук завершился");
audioSource.OnLoaded += (key) => Debug.Log($"Звук {key} загружен");
audioSource.OnError += (key, error) => Debug.Log($"Ошибка: {error}");
audioSource.OnPlay += () => Debug.Log("Звук начал воспроизводиться");
audioSource.OnStop += () => Debug.Log("Звук остановлен");
audioSource.OnMute += (muted) => Debug.Log($"Звук {(muted ? "заглушен" : "включен")}");
```

### Глобальное управление

```csharp
// Управление глобальной громкостью
AudioManagement.Instance.SetVolume(0.7f);
```

## Основные возможности

- Двойная система провайдеров (Unity/JavaScript)
- Кэширование аудио
- Управление глобальной громкостью
- Система событий
- WebGL интеграция
- Управление позицией воспроизведения
- Приоритеты звуков

## Начало работы

### 1. Настройка базы данных

1. Создайте базу данных через меню: `Assets/Create/FoundersPlugin/Audio/Database`
2. Поместите созданный файл в папку `Resources`
3. Добавьте аудио клипы в базу данных через редактор, указав:
   - Ключ (уникальный идентификатор звука)
   - Путь к файлу
   - Параметры предзагрузки

### 2. Добавление источника звука

```csharp
// Добавьте компонент SourceAudio на GameObject
var audioSource = gameObject.AddComponent<SourceAudio>();

// Выберите тип провайдера
audioSource.providerType = AudioProviderType.Unity; // или AudioProviderType.JS для WebGL
```

### 3. Воспроизведение звуков

```csharp
// Асинхронное воспроизведение
await audioSource.Play("sound_key");

// Воспроизведение поверх текущего звука (только для Unity провайдера)
await audioSource.PlayOneShot("sound_key");

// Остановка
audioSource.Stop();

// Пауза/Возобновление
audioSource.Pause();
audioSource.UnPause();
```

### 4. Управление громкостью

```csharp
// Установка громкости для конкретного источника
audioSource.Volume = 0.5f;

// Глобальная громкость
AudioManagement.Instance.SetVolume(0.7f);
```

## Компоненты модуля

### AudioModule

Основной класс модуля, отвечающий за:
- Инициализацию системы
- Загрузку и кэширование аудио файлов
- Управление настройками

### AudioManagement

Синглтон для глобального управления аудио системой:
- Управление глобальной громкостью
- Регистрация источников звука
- Групповые операции (остановка/пауза всех звуков)

### SourceAudio

Компонент для воспроизведения звуков:
- Поддержка двух провайдеров (Unity/JS)
- Управление параметрами воспроизведения
- События завершения воспроизведения

### AudioDatabase

База данных для хранения информации об аудио клипах:
- Ключи и пути к файлам
- Параметры предзагрузки
- Дополнительные настройки (громкость по умолчанию, зацикливание)

### WebGLAudioBridge

Мост между C# и JavaScript кодом для WebGL платформы:
- Инициализация аудио системы
- Управление воспроизведением
- Управление громкостью

## Настройка WebGL

Модуль автоматически:
- Внедряет необходимые скрипты в WebGL шаблон
- Копирует аудио файлы в билд
- Настраивает взаимодействие с JavaScript

## События

Модуль предоставляет следующие события:
- OnSoundLoaded - звук загружен
- OnSoundFailed - ошибка загрузки
- OnSoundPlayed - звук начал воспроизводиться
- OnSoundStopped - звук остановлен
- OnFinished - воспроизведение завершено

## Оптимизация

### Кэширование

Модуль автоматически кэширует загруженные звуки:
- Настраиваемый размер кэша
- Время жизни кэшированных файлов
- Автоматическая очистка при превышении лимита

### Предзагрузка

Для оптимизации производительности:
1. Отметьте часто используемые звуки для предзагрузки в базе данных
2. Звуки будут автоматически загружены при старте
3. Используйте `PreloadSounds()` для ручной предзагрузки

## Примеры использования

### Простое воспроизведение звука

```csharp
public class SoundExample : MonoBehaviour
{
    private SourceAudio audioSource;

    private void Start()
    {
        audioSource = GetComponent<SourceAudio>();
    }

    public async void PlaySound()
    {
        await audioSource.Play("sound_key");
    }
}
```

### Управление громкостью с сохранением

```csharp
public class VolumeController : MonoBehaviour
{
    public void SetVolume(float volume)
    {
        AudioManagement.Instance.SetVolume(volume);
        PlayerPrefs.SetFloat("AudioVolume", volume);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("AudioVolume", 1f);
        AudioManagement.Instance.SetVolume(savedVolume);
    }
}
```

### Система звуков интерфейса

```csharp
public class UIAudioSystem : MonoBehaviour
{
    private SourceAudio effectSource;

    private void Start()
    {
        effectSource = gameObject.AddComponent<SourceAudio>();
    }

    public void PlayButtonClick()
    {
        effectSource.PlayOneShot("button_click");
    }

    public void PlaySuccess()
    {
        effectSource.PlayOneShot("success_sound");
    }
}
```

### Сохранение между сценами

Для создания фоновой музыки, которая продолжает играть при переходе между сценами:

```csharp
public class BackgroundMusic : MonoBehaviour
{
    private SourceAudio musicSource;

    private async void Start()
    {
        musicSource = gameObject.AddComponent<SourceAudio>();
        
        // Воспроизведение музыки с сохранением между сценами
        await musicSource.PlayWithParameters("backgroundMusic", new PlaySoundParameters
        {
            loop = true,
            dontDestroyOnLoad = true,
            volume = 0.7f
        });
    }
}
```

При использовании `dontDestroyOnLoad = true`:
- Объект со звуком не будет уничтожен при смене сцены
- Позиция воспроизведения сохранится
- Все параметры (громкость, pitch) будут сохранены
- Воспроизведение продолжится с той же позиции

## Устранение неполадок

### Звук не воспроизводится в WebGL

1. Проверьте, что файл добавлен в базу данных
2. Убедитесь, что путь к файлу корректный
3. Проверьте консоль браузера на наличие ошибок
4. Убедитесь, что howler.js и WebAudio.js правильно подключены

### Проблемы с громкостью

1. Проверьте глобальную громкость через AudioManagement
2. Проверьте локальную громкость источника
3. Убедитесь, что звук не заглушен (Mute)

### Ошибки загрузки

1. Проверьте, что файл находится в папке Resources
2. Убедитесь, что формат файла поддерживается
3. Проверьте путь в базе данных 

## Тестирование

### Простой тест проигрывания

```csharp
public class AudioTest : MonoBehaviour
{
    private SourceAudio audioSource;
    
    private void Start()
    {
        // Создаем источник звука
        audioSource = gameObject.AddComponent<SourceAudio>();
        
        // Тестируем воспроизведение
        TestPlayback();
    }
    
    private async void TestPlayback()
    {
        // Тест простого воспроизведения
        await audioSource.Play("testSound");
        Debug.Log("Звук начал воспроизводиться");
        
        // Проверяем свойства
        Debug.Log($"Громкость: {audioSource.Volume}");
        Debug.Log($"Играет: {audioSource.IsPlaying}");
        
        // Ждем 2 секунды
        await Task.Delay(2000);
        
        // Останавливаем
        audioSource.Stop();
        Debug.Log("Звук остановлен");
    }
}
```

### Тест с событиями

```csharp
public class AudioEventTest : MonoBehaviour
{
    private SourceAudio audioSource;
    
    private void Start()
    {
        audioSource = gameObject.AddComponent<SourceAudio>();
        
        // Подписываемся на события
        audioSource.OnLoaded += (key) => Debug.Log($"Загружен: {key}");
        audioSource.OnFinished += () => Debug.Log("Завершен");
        audioSource.OnError += (key, error) => Debug.Log($"Ошибка: {error}");
        
        // Запускаем тест
        TestEvents();
    }
    
    private async void TestEvents()
    {
        // Тестируем загрузку и воспроизведение
        await audioSource.Play("testSound");
        
        // Тестируем изменение параметров
        audioSource.Volume = 0.5f;
        audioSource.Pitch = 1.2f;
        
        // Тестируем паузу/воспроизведение
        await Task.Delay(1000);
        audioSource.Pause();
        
        await Task.Delay(1000);
        audioSource.UnPause();
    }
}
```

### Тест сохранения между сценами

```csharp
public class AudioPersistenceTest : MonoBehaviour
{
    private SourceAudio audioSource;
    
    private void Start()
    {
        audioSource = gameObject.AddComponent<SourceAudio>();
        
        // Запускаем фоновую музыку, которая должна сохраниться при смене сцены
        PlayBackgroundMusic();
    }
    
    private async void PlayBackgroundMusic()
    {
        await audioSource.PlayLooped("backgroundMusic", dontDestroyOnLoad: true);
        
        // Теперь можно загружать другую сцену
        // SceneManager.LoadScene("NextScene");
    }
}
```

### Тест параметров воспроизведения

```csharp
public class AudioParametersTest : MonoBehaviour
{
    private SourceAudio audioSource;
    
    private async void Start()
    {
        audioSource = gameObject.AddComponent<SourceAudio>();
        
        // Тестируем различные параметры
        var parameters = new PlaySoundParameters
        {
            volume = 0.8f,
            pitch = 1.2f,
            startTime = 5f,
            loop = true,
            dontDestroyOnLoad = true
        };
        
        await audioSource.PlayWithParameters("testSound", parameters);
        
        // Проверяем, что параметры применились
        Debug.Assert(audioSource.Volume == 0.8f, "Громкость не установлена");
        Debug.Assert(audioSource.Pitch == 1.2f, "Pitch не установлен");
        Debug.Assert(audioSource.Loop == true, "Loop не установлен");
    }
}

### Тест управления позицией воспроизведения

```csharp
public class AudioTimeTest : MonoBehaviour
{
    private SourceAudio audioSource;
    
    private async void Start()
    {
        audioSource = gameObject.AddComponent<SourceAudio>();
        
        // Воспроизводим звук
        await audioSource.Play("testSound");
        
        // Получаем текущую позицию
        float currentTime = audioSource.Time;
        Debug.Log($"Текущая позиция: {currentTime}");
        
        // Перематываем на середину
        // Time автоматически ограничивается длиной клипа
        audioSource.Time = currentTime + 5f;
        
        // Пример работы со слайдером времени
        public void OnTimeSliderChanged(float value)
        {
            if (audioSource != null && audioSource.IsPlaying)
            {
                // Значение слайдера должно быть нормализовано (0-1)
                float clipLength = GetClipLength(); // Получите длину клипа
                audioSource.Time = value * clipLength;
            }
        }
        
        private float GetClipLength()
        {
            var audioSource = GetComponent<AudioSource>();
            return audioSource != null && audioSource.clip != null ? audioSource.clip.length : 0f;
        }
    }
} 