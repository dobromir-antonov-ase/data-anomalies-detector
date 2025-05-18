import { Component, OnInit, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { QueryBuilderService, QueryResponse, SpeechToQueryResponse } from './query-builder.service';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-query-builder',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './query-builder.component.html',
  styleUrls: ['./query-builder.component.scss']
})
export class QueryBuilderComponent implements OnInit {
  queryText: string = '';
  generatedQuery: string = '';
  queryTypes: string[] = [];
  selectedQueryType: string = 'sql';
  isRecording: boolean = false;
  queryResult: any[] = [];
  isLoading: boolean = false;
  errorMessage: string = '';
  mediaRecorder: MediaRecorder | null = null;
  audioChunks: Blob[] = [];
  // Audio recording settings
  audioContext: AudioContext | null = null;
  audioStream: MediaStream | null = null;

  constructor(
    private queryBuilderService: QueryBuilderService,
    @Inject(DOCUMENT) private document: Document
  ) {}

  ngOnInit(): void {
    this.loadQueryTypes();
    // Initialize audio context
    this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
  }

  loadQueryTypes(): void {
    this.queryBuilderService.getQueryTypes().subscribe({
      next: (types: string[]) => {
        this.queryTypes = types;
        if (types.length > 0) {
          this.selectedQueryType = types[0];
        }
      },
      error: (error: any) => {
        console.error('Error loading query types', error);
        this.errorMessage = 'Failed to load query types';
      }
    });
  }

  generateQuery(): void {
    if (!this.queryText) {
      this.errorMessage = 'Please enter a query description';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.generatedQuery = '';
    this.queryResult = [];

    this.queryBuilderService.generateQuery({
      naturalLanguageQuery: this.queryText,
      queryType: this.selectedQueryType
    }).subscribe({
      next: (response: QueryResponse) => {
        this.generatedQuery = response.generatedQuery;
        this.queryResult = response.previewData || [];
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error generating query', error);
        this.errorMessage = 'Failed to generate query';
        this.isLoading = false;
      }
    });
  }

  startRecording(): void {
    this.isRecording = true;
    this.audioChunks = [];
    
    navigator.mediaDevices.getUserMedia({ audio: true })
      .then(stream => {
        this.audioStream = stream;
        this.mediaRecorder = new MediaRecorder(stream);
        this.mediaRecorder.addEventListener('dataavailable', (event) => {
          this.audioChunks.push(event.data);
        });

        this.mediaRecorder.addEventListener('stop', () => {
          // Create a proper WAV file with headers
          this.createWavFromBlobs(this.audioChunks).then(wavBlob => {
            this.processAudioInput(wavBlob);
          });
          
          // Stop all tracks to release the microphone
          stream.getTracks().forEach(track => track.stop());
        });

        this.mediaRecorder.start();
      })
      .catch(error => {
        console.error('Error accessing microphone', error);
        this.errorMessage = 'Could not access microphone. Please check permissions.';
        this.isRecording = false;
      });
  }

  stopRecording(): void {
    if (this.mediaRecorder && this.isRecording) {
      this.mediaRecorder.stop();
      this.isRecording = false;
    }
  }

  processAudioInput(audioBlob: Blob): void {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.queryBuilderService.processAudioQuery(audioBlob, this.selectedQueryType)
      .subscribe({
        next: (response: SpeechToQueryResponse) => {
          this.queryText = response.transcribedText;
          this.generatedQuery = response.query.generatedQuery;
          this.queryResult = response.query.previewData || [];
          this.isLoading = false;
        },
        error: (error: any) => {
          console.error('Error processing speech', error);
          this.errorMessage = 'Failed to process speech input';
          this.isLoading = false;
        }
      });
  }
  
  // Helper method to get headers from query result
  getHeaders(): string[] {
    if (!this.queryResult || this.queryResult.length === 0) {
      return [];
    }
    
    // Get all unique keys from all result objects
    return Object.keys(this.queryResult[0]);
  }
  
  // Method to copy text to clipboard
  copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text)
      .then(() => {
        // Could add a toast notification here
        console.log('Copied to clipboard');
      })
      .catch(err => {
        console.error('Failed to copy text: ', err);
      });
  }

  // Helper method to properly format audio data as WAV
  async createWavFromBlobs(audioChunks: Blob[]): Promise<Blob> {
    // Combine all audio chunks into one blob
    const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
    
    // Convert blob to array buffer
    const arrayBuffer = await audioBlob.arrayBuffer();
    
    // Decode the audio data
    const audioBuffer = await this.audioContext!.decodeAudioData(arrayBuffer);
    
    // Convert to WAV format with proper headers
    const wavBlob = this.audioBufferToWav(audioBuffer);
    
    return wavBlob;
  }
  
  // Convert AudioBuffer to WAV format
  audioBufferToWav(audioBuffer: AudioBuffer): Blob {
    const numOfChannels = audioBuffer.numberOfChannels;
    const sampleRate = audioBuffer.sampleRate;
    const format = 1; // PCM
    const bitDepth = 16;
    
    const bytesPerSample = bitDepth / 8;
    const blockAlign = numOfChannels * bytesPerSample;
    
    const buffer = audioBuffer.getChannelData(0);
    const dataLength = buffer.length * bytesPerSample;
    const totalLength = 44 + dataLength;
    
    const arrayBuffer = new ArrayBuffer(totalLength);
    const dataView = new DataView(arrayBuffer);
    
    // RIFF identifier
    this.writeString(dataView, 0, 'RIFF');
    // RIFF chunk length
    dataView.setUint32(4, 36 + dataLength, true);
    // RIFF type
    this.writeString(dataView, 8, 'WAVE');
    // Format chunk identifier
    this.writeString(dataView, 12, 'fmt ');
    // Format chunk length
    dataView.setUint32(16, 16, true);
    // Sample format (raw)
    dataView.setUint16(20, format, true);
    // Channel count
    dataView.setUint16(22, numOfChannels, true);
    // Sample rate
    dataView.setUint32(24, sampleRate, true);
    // Byte rate (sample rate * block align)
    dataView.setUint32(28, sampleRate * blockAlign, true);
    // Block align (channel count * bytes per sample)
    dataView.setUint16(32, blockAlign, true);
    // Bits per sample
    dataView.setUint16(34, bitDepth, true);
    // Data chunk identifier
    this.writeString(dataView, 36, 'data');
    // Data chunk length
    dataView.setUint32(40, dataLength, true);
    
    // Write the PCM samples
    const offset = 44;
    for (let i = 0; i < buffer.length; i++) {
      const sample = Math.max(-1, Math.min(1, buffer[i]));
      // Scale to 16-bit range
      const sampleValue = sample < 0 ? sample * 32768 : sample * 32767;
      // Write 16-bit sample
      dataView.setInt16(offset + i * 2, sampleValue, true);
    }
    
    return new Blob([arrayBuffer], { type: 'audio/wav' });
  }
  
  // Helper to write a string to a DataView
  writeString(dataView: DataView, offset: number, string: string): void {
    for (let i = 0; i < string.length; i++) {
      dataView.setUint8(offset + i, string.charCodeAt(i));
    }
  }
} 