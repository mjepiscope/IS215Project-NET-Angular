import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { Observable } from "rxjs";
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

  public uploadImage(file: File): Observable<number> {

    let formData: FormData = new FormData();
    formData.append('file', file);

    return this.http.post<number>(
      '/api/aws/uploadImage',
      formData
    );
  }

  public getGeneratedContent(timestamp: number): Observable<string> {
    let params =
      new HttpParams()
        .append("timestamp", timestamp);

    return this.http.get<string>(
      '/api/aws/getGeneratedContent',
      { params }
    );
  }
}
