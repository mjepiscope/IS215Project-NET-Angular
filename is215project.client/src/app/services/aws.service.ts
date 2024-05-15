import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { Observable } from "rxjs";
import { S3Bucket } from '../models/s3-bucket';
import { Article } from '../models/article';

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

  //public generateContentFromImage(file: File): Observable<string> {

  //  let formData: FormData = new FormData();
  //  formData.append('file', file);

  //  return this.http.post<string>(
  //    '/api/aws/generateContentFromImage',
  //    formData
  //  );

  //}

  public uploadImage(file: File): Observable<Article> {

    let formData: FormData = new FormData();
    formData.append('file', file);

    return this.http.post<Article>(
      '/api/aws/uploadImage',
      formData
    );

  }

  public getGeneratedContent(filename: string): Observable<string> {
    let params =
      new HttpParams()
        .append("filename", filename);

    return this.http.get<string>(
      '/api/aws/getGeneratedContent',
      { params }
    );
  }
}
