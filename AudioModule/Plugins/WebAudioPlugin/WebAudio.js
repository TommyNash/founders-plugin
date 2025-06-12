var WebAudio = {
    // Хранилище для звуков
    sounds: {},
    
    // Глобальные настройки
    globalVolume: 1.0,
    
    isUserInteracted: false,
    deferredSounds: [], // Звуки, ожидающие взаимодействия
    maxSimultaneousSounds: 8, // Максимальное количество одновременных звуков
    activeSounds: new Map(), // Активные звуки с их приоритетами
    audioContext: null, // Web Audio API context
    masterGainNode: null, // Основной узел громкости
    listener: null, // Слушатель для пространственного звука
    
    // Инициализация аудио системы
    InitializeAudioSystem: function(volume) {
        WebAudio.globalVolume = volume;
        
        try {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            WebAudio.audioContext = new AudioContext();
            
            // Создаем основной узел громкости
            WebAudio.masterGainNode = WebAudio.audioContext.createGain();
            WebAudio.masterGainNode.connect(WebAudio.audioContext.destination);
            WebAudio.masterGainNode.gain.value = volume;
            
            // Получаем слушателя
            WebAudio.listener = WebAudio.audioContext.listener;
        } catch (e) {
            console.error('Web Audio API not supported:', e);
            return;
        }
        
        // Регистрируем обработчик взаимодействия
        const userInteractionHandler = function() {
            if (!WebAudio.isUserInteracted) {
                WebAudio.isUserInteracted = true;
                if (WebAudio.audioContext.state === 'suspended') {
                    WebAudio.audioContext.resume();
                }
                WebAudio.playDeferredSounds();
                // Удаляем обработчики после первого взаимодействия
                ['click', 'touchstart', 'keydown'].forEach(event => {
                    document.removeEventListener(event, userInteractionHandler);
                });
            }
        };
        
        ['click', 'touchstart', 'keydown'].forEach(event => {
            document.addEventListener(event, userInteractionHandler);
        });
        
        // Обработчики видимости и фокуса
        document.addEventListener('visibilitychange', function() {
            try {
                if (document.hidden) {
                    if (typeof WebAudio.PauseAll === 'function') {
                        WebAudio.PauseAll.call(WebAudio);
                    } else {
                        console.warn('WebAudio.PauseAll is not available');
                    }
                } else {
                    if (typeof WebAudio.UnpauseAll === 'function') {
                        WebAudio.UnpauseAll.call(WebAudio);
                    } else {
                        console.warn('WebAudio.UnpauseAll is not available');
                    }
                }
            } catch (error) {
                console.error('Error in visibilitychange handler:', error);
            }
        });
        
        window.addEventListener('blur', () => {
            try {
                if (typeof WebAudio.PauseAll === 'function') {
                    WebAudio.PauseAll.call(WebAudio);
                } else {
                    console.warn('WebAudio.PauseAll is not available');
                }
            } catch (error) {
                console.error('Error in blur handler:', error);
            }
        });
        
        window.addEventListener('focus', () => {
            try {
                if (typeof WebAudio.UnpauseAll === 'function') {
                    WebAudio.UnpauseAll.call(WebAudio);
                } else {
                    console.warn('WebAudio.UnpauseAll is not available');
                }
            } catch (error) {
                console.error('Error in focus handler:', error);
            }
        });
    },
    
    // Создание источника звука
    createAudioSource: function(buffer, options = {}) {
        const source = WebAudio.audioContext.createBufferSource();
        source.buffer = buffer;
        
        // Создаем узлы для управления звуком
        const gainNode = WebAudio.audioContext.createGain();
        let pannerNode = null;
        
        // Настраиваем пространственный звук если нужно
        if (options.spatial) {
            pannerNode = WebAudio.audioContext.createPanner();
            pannerNode.panningModel = 'HRTF';
            pannerNode.distanceModel = 'inverse';
            pannerNode.refDistance = 1;
            pannerNode.maxDistance = 10000;
            pannerNode.rolloffFactor = 1;
            pannerNode.coneInnerAngle = 360;
            pannerNode.coneOuterAngle = 360;
            pannerNode.coneOuterGain = 0;
            
            // Подключаем узлы
            source.connect(pannerNode);
            pannerNode.connect(gainNode);
        } else {
            source.connect(gainNode);
        }
        
        gainNode.connect(WebAudio.masterGainNode);
        
        // Настраиваем параметры
        source.playbackRate.value = options.pitch || 1;
        gainNode.gain.value = (options.volume || 1) * WebAudio.globalVolume;
        
        if (options.loop) source.loop = true;
        
        return {
            source: source,
            gainNode: gainNode,
            pannerNode: pannerNode,
            startTime: 0,
            pausedAt: 0,
            isPlaying: false,
            buffer: buffer
        };
    },
    
    // Загрузка звука
    LoadSound: function(key, path, defaultVolume, pitch, spatial = false) {
        return new Promise((resolve, reject) => {
            if (WebAudio.sounds[key]) {
                resolve(key);
                return;
            }
            
            fetch(path)
                .then(response => response.arrayBuffer())
                .then(arrayBuffer => WebAudio.audioContext.decodeAudioData(arrayBuffer))
                .then(audioBuffer => {
                    WebAudio.sounds[key] = {
                        buffer: audioBuffer,
                        volume: defaultVolume,
                        pitch: pitch,
                        spatial: spatial,
                        instances: new Map() // Хранит активные источники звука
                    };
                    if (WebAudio.onSoundLoaded) {
                        WebAudio.onSoundLoaded(key);
                    }
                    resolve(key);
                })
                .catch(error => {
                    console.error(`Error loading sound ${key}:`, error);
                    if (WebAudio.onSoundError) {
                        WebAudio.onSoundError(key, error);
                    }
                    reject(error);
                });
        });
    },
    
    // Проигрывание звука
    PlaySound: function(key, startTime = 0, volume = 1, pitch = 1, priority = 0, position = null) {
        const sound = WebAudio.sounds[key];
        if (!sound) return -1;
        
        // Если нет взаимодействия - откладываем
        if (!WebAudio.isUserInteracted) {
            WebAudio.deferredSounds.push({key, startTime, volume, pitch, priority, position});
            return -1;
        }
        
        // Проверяем возможность проигрывания
        if (!WebAudio.manageActiveSounds(key, priority)) {
            console.warn(`Cannot play sound ${key}: too many active sounds`);
            return -1;
        }
        
        try {
            const id = Date.now(); // Уникальный ID для этого экземпляра
            const instance = WebAudio.createAudioSource(sound.buffer, {
                volume: volume,
                pitch: pitch,
                spatial: sound.spatial
            });
            
            // Сохраняем экземпляр
            sound.instances.set(id, instance);
            
            // Устанавливаем позицию если нужно
            if (position && instance.pannerNode) {
                instance.pannerNode.setPosition(position.x, position.y, position.z);
            }
            
            // Запускаем воспроизведение
            instance.source.start(0, startTime);
            instance.startTime = WebAudio.audioContext.currentTime - startTime;
            instance.isPlaying = true;
            
            // Добавляем обработчик окончания
            instance.source.onended = () => {
                sound.instances.delete(id);
                WebAudio.activeSounds.delete(key);
                if (WebAudio.onSoundComplete) {
                    WebAudio.onSoundComplete(key);
                }
            };
            
            return id;
        } catch (error) {
            console.error(`Error playing sound ${key}:`, error);
            WebAudio.activeSounds.delete(key);
            return -1;
        }
    },
    
    // Остановка звука
    StopSound: function(key, id) {
        const sound = WebAudio.sounds[key];
        if (!sound) return;
        
        if (id >= 0) {
            const instance = sound.instances.get(id);
            if (instance) {
                instance.source.stop();
                instance.isPlaying = false;
                sound.instances.delete(id);
            }
        } else {
            sound.instances.forEach(instance => {
                instance.source.stop();
                instance.isPlaying = false;
            });
            sound.instances.clear();
        }
    },
    
    // Пауза
    PauseSound: function(key, id) {
        const sound = WebAudio.sounds[key];
        if (!sound) return;
        
        const pauseInstance = (instance) => {
            if (instance.isPlaying) {
                instance.pausedAt = WebAudio.audioContext.currentTime - instance.startTime;
                instance.source.stop();
                instance.isPlaying = false;
            }
        };
        
        if (id >= 0) {
            const instance = sound.instances.get(id);
            if (instance) pauseInstance(instance);
        } else {
            sound.instances.forEach(pauseInstance);
        }
    },
    
    // Снятие с паузы
    UnpauseSound: function(key, id) {
        const sound = WebAudio.sounds[key];
        if (!sound) return;
        
        const unpauseInstance = (instance, instanceId) => {
            if (!instance.isPlaying && instance.pausedAt > 0) {
                const newInstance = WebAudio.createAudioSource(sound.buffer, {
                    volume: instance.gainNode.gain.value,
                    pitch: instance.source.playbackRate.value,
                    spatial: sound.spatial
                });
                
                newInstance.startTime = WebAudio.audioContext.currentTime - instance.pausedAt;
                newInstance.source.start(0, instance.pausedAt);
                newInstance.isPlaying = true;
                
                sound.instances.set(instanceId, newInstance);
            }
        };
        
        if (id >= 0) {
            const instance = sound.instances.get(id);
            if (instance) unpauseInstance(instance, id);
        } else {
            sound.instances.forEach(unpauseInstance);
        }
    },
    
    // Установка громкости
    SetVolume: function(key, volume) {
        const sound = WebAudio.sounds[key];
        if (sound) {
            sound.instances.forEach(instance => {
                instance.gainNode.gain.value = volume * WebAudio.globalVolume;
            });
        }
    },
    
    // Установка глобальной громкости
    SetGlobalVolume: function(volume) {
        WebAudio.globalVolume = Math.min(1, Math.max(0, volume));
        WebAudio.masterGainNode.gain.value = WebAudio.globalVolume;
    },
    
    // Установка позиции слушателя
    SetListenerPosition: function(x, y, z) {
        // Проверка на конечные числа
        if (![x, y, z].every(Number.isFinite)) {
            console.warn("SetListenerPosition: получены не-конечные значения", {x, y, z});
            return;
        }

        if (WebAudio.listener) {
            if (WebAudio.listener.positionX) {
                WebAudio.listener.positionX.value = x;
                WebAudio.listener.positionY.value = y;
                WebAudio.listener.positionZ.value = z;
            } else {
                WebAudio.listener.setPosition(x, y, z);
            }
        }
    },
    
    // Установка ориентации слушателя
    SetListenerOrientation: function(x, y, z, forwardX, forwardY, forwardZ, upX, upY, upZ) {
        // Проверка на конечные числа
        const values = [x, y, z, forwardX, forwardY, forwardZ, upX, upY, upZ];
        if (!values.every(Number.isFinite)) {
            console.warn("SetListenerOrientation: получены не-конечные значения", {
                position: {x, y, z},
                forward: {x: forwardX, y: forwardY, z: forwardZ},
                up: {x: upX, y: upY, z: upZ}
            });
            return;
        }

        if (WebAudio.listener) {
            if (WebAudio.listener.forwardX) {
                WebAudio.listener.forwardX.value = forwardX;
                WebAudio.listener.forwardY.value = forwardY;
                WebAudio.listener.forwardZ.value = forwardZ;
                WebAudio.listener.upX.value = upX;
                WebAudio.listener.upY.value = upY;
                WebAudio.listener.upZ.value = upZ;
            } else {
                WebAudio.listener.setOrientation(forwardX, forwardY, forwardZ, upX, upY, upZ);
            }
        }
    },
    
    // Установка позиции источника звука
    SetPosition: function(key, x, y, z, id) {
        // Проверка на конечные числа
        if (![x, y, z].every(Number.isFinite)) {
            console.warn("SetPosition: получены не-конечные значения", {key, x, y, z, id});
            return;
        }

        const sound = WebAudio.sounds[key];
        if (!sound) return;
        
        if (id >= 0) {
            const instance = sound.instances.get(id);
            if (instance && instance.pannerNode) {
                instance.pannerNode.setPosition(x, y, z);
            }
        } else {
            sound.instances.forEach(instance => {
                if (instance.pannerNode) {
                    instance.pannerNode.setPosition(x, y, z);
                }
            });
        }
    },
    
    // Управление активными звуками
    manageActiveSounds: function(key, priority) {
        if (WebAudio.activeSounds.size >= WebAudio.maxSimultaneousSounds) {
            let lowestPriority = priority;
            let soundToStop = null;
            
            WebAudio.activeSounds.forEach((value, activeKey) => {
                if (value < lowestPriority) {
                    lowestPriority = value;
                    soundToStop = activeKey;
                }
            });
            
            if (soundToStop && lowestPriority < priority) {
                WebAudio.StopSound(soundToStop);
                WebAudio.activeSounds.delete(soundToStop);
            } else {
                return false;
            }
        }
        
        WebAudio.activeSounds.set(key, priority);
        return true;
    },
    
    // Проигрывание отложенных звуков
    playDeferredSounds: function() {
        while (WebAudio.deferredSounds.length > 0) {
            const sound = WebAudio.deferredSounds.shift();
            WebAudio.PlaySound(
                sound.key,
                sound.startTime,
                sound.volume,
                sound.pitch,
                sound.priority,
                sound.position
            );
        }
    },
    
    // Очистка ресурсов
    Dispose: function(key) {
        const sound = WebAudio.sounds[key];
        if (sound) {
            sound.instances.forEach(instance => {
                if (instance.isPlaying) {
                    instance.source.stop();
                }
            });
            sound.instances.clear();
            delete WebAudio.sounds[key];
        }
    },
    
    // События
    onSoundLoaded: null,
    onSoundError: null,
    onSoundComplete: null,
    onMuteChanged: null,

    // Глобальная пауза всех звуков
    PauseAll: function() {
        try {
            if (!this.sounds) {
                console.warn('WebAudio.sounds is not available');
                return;
            }
            
            Object.keys(this.sounds).forEach(key => {
                const sound = this.sounds[key];
                if (sound && sound.instances) {
                    sound.instances.forEach((instance, id) => {
                        if (instance && instance.isPlaying) {
                            this.PauseSound(key, id);
                        }
                    });
                }
            });
        } catch (error) {
            console.error('Error in PauseAll:', error);
        }
    },

    // Глобальное возобновление всех звуков
    UnpauseAll: function() {
        try {
            if (!this.sounds) {
                console.warn('WebAudio.sounds is not available');
                return;
            }
            
            Object.keys(this.sounds).forEach(key => {
                const sound = this.sounds[key];
                if (sound && sound.instances) {
                    sound.instances.forEach((instance, id) => {
                        if (instance && !instance.isPlaying && instance.pausedAt > 0) {
                            this.UnpauseSound(key, id);
                        }
                    });
                }
            });
        } catch (error) {
            console.error('Error in UnpauseAll:', error);
        }
    }
}; 