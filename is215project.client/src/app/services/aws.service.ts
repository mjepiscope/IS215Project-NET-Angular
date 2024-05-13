import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { Observable, of } from "rxjs";
import { S3Bucket } from '../models/s3-bucket';

@Injectable({
  providedIn: 'root'
})
export class AwsService {
  constructor(private http: HttpClient) { }

  public testConnection(): Observable<boolean> {
    return this.http.get<boolean>('/api/aws/testConnection');
  }

  public getBuckets(): Observable<S3Bucket[]> {
    return this.http.get<S3Bucket[]>('/api/aws/getBuckets');
  }

  public generateContentFromImage(image: any): Observable<string> {
    return of("Lorem Ipsum...");
  }
}
