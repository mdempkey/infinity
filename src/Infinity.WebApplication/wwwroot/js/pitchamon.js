(function () {
    'use strict';

    let activeSampler = null;
    let activePart = null;

    // Autocorrelation pitch detection.
    // Returns the nearest MIDI note number for the dominant frequency in audioBuffer.
    function detectPitch(audioBuffer) {
        const sampleRate = audioBuffer.sampleRate;
        const channelData = audioBuffer.getChannelData(0);
        const offset = Math.floor(sampleRate * 0.2);
        const sampleCount = Math.min(Math.floor(sampleRate * 0.2), Math.max(0, channelData.length - offset));

        // Search range: 80 Hz (maxLag) to 2000 Hz (minLag)
        const minLag = Math.floor(sampleRate / 2000);
        const maxLag = Math.floor(sampleRate / 80);

        let bestLag = minLag;
        let bestCorrelation = -Infinity;

        for (let lag = minLag; lag <= maxLag; lag++) {
            let sum = 0;
            const limit = sampleCount - lag;
            for (let i = 0; i < limit; i++) {
                sum += channelData[offset + i] * channelData[offset + i + lag];
            }
            if (sum > bestCorrelation) {
                bestCorrelation = sum;
                bestLag = lag;
            }
        }

        const frequency = sampleRate / bestLag;
        return Math.round(12 * Math.log2(frequency / 440) + 69);
    }

    // Fetches a cry .wav, decodes it, detects its pitch, and returns a ready Tone.Sampler.
    async function createSampler(url) {
        const response = await fetch(url);
        const arrayBuffer = await response.arrayBuffer();
        const audioBuffer = await Tone.context.decodeAudioData(arrayBuffer);
        const baseNote = detectPitch(audioBuffer);
        const baseNoteStr = Tone.Frequency(baseNote, 'midi').toNote();

        return new Promise((resolve, reject) => {
            const sampler = new Tone.Sampler({
                urls: { [baseNoteStr]: url },
                attack: 0.005,
                release: 0.05,
                onload: () => resolve(sampler),
                onerror: reject
            }).toDestination();
        });
    }

    // Builds a Tone.Part from the note array returned by /audio/notes.
    function buildPart(notes) {
        if (activePart) {
            activePart.dispose();
        }
        const events = notes.map(n => [n.time, { midi: n.midi, duration: n.duration }]);
        activePart = new Tone.Part((time, value) => {
            if (activeSampler) {
                activeSampler.triggerAttackRelease(
                    Tone.Frequency(value.midi, 'midi').toNote(),
                    value.duration,
                    time
                );
            }
        }, events);
        activePart.start(0);
    }

    async function init() {
        const [notesData, sampler] = await Promise.all([
            fetch('/audio/notes').then(r => r.json()),
            createSampler('/audio/cry-default.wav')
        ]);

        activeSampler = sampler;
        buildPart(notesData.notes);

        Tone.Transport.loop = true;
        Tone.Transport.loopStart = notesData.loopStart;
        Tone.Transport.loopEnd = notesData.totalDuration;

        const startAudio = async () => {
            await Tone.start();
            Tone.Transport.start();
            document.removeEventListener('mousedown', startAudio);
            document.removeEventListener('click', startAudio);
            document.removeEventListener('keydown', startAudio);
        };
        document.addEventListener('mousedown', startAudio);
        document.addEventListener('click', startAudio);
        document.addEventListener('keydown', startAudio);
    }

    // Public API — exposed for the future hot-swap dropdown feature.
    window.Pitchamon = {
        swapCry: async function (url) {
            const newSampler = await createSampler(url);
            const old = activeSampler;
            activeSampler = newSampler;
            if (old) setTimeout(() => old.dispose(), 2000);
        }
    };

    init().catch(err => console.error('[Pitchamon] init failed:', err));
})();
