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

  constructor(
    private queryBuilderService: QueryBuilderService,
    @Inject(DOCUMENT) private document: Document
  ) {}

  ngOnInit(): void {
    this.loadQueryTypes();
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
        this.mediaRecorder = new MediaRecorder(stream);
        this.mediaRecorder.addEventListener('dataavailable', (event) => {
          this.audioChunks.push(event.data);
        });

        this.mediaRecorder.addEventListener('stop', () => {
          const audioBlob = new Blob(this.audioChunks, { type: 'audio/wav' });
          this.processAudioInput(audioBlob);
          
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
} 