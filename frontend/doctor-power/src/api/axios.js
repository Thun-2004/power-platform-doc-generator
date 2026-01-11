import axios from 'axios';

const BASE_URL = process.env.POWERDOCU_BACKEND_URL || 'http://localhost:8000';

export default axios.create({
    baseURL: BASE_URL
})

export const axiosPrivate = axios.create({
    baseURL: BASE_URL,
    withCredentials: true
})




//temp
// import { useRef, useState } from "react";
// import axios from "axios";

// // If you have axiosPrivate, use it here instead of axios
// // import { axiosPrivate } from "./axiosPrivate";

// export default function GeneratorUpload() {
//   const [file, setFile] = useState(null);          // File | null
//   const [modes, setModes] = useState([]);          // string[]
//   const [progress, setProgress] = useState(0);
//   const [isUploading, setIsUploading] = useState(false);

//   const abortRef = useRef(null);                   // AbortController | null

//   const onPickFile = (e) => {
//     const f = e.target.files?.[0] ?? null;
//     setFile(f);
//     setProgress(0);
//   };

//   const onRemoveFile = () => {
//     // "Delete" from frontend buffer
//     setFile(null);
//     setProgress(0);
//   };

//   const toggleMode = (mode) => {
//     setModes((prev) =>
//       prev.includes(mode) ? prev.filter((m) => m !== mode) : [...prev, mode]
//     );
//   };

//   const onGenerate = async () => {
//     if (!file) return;

//     const fd = new FormData();
//     fd.append("file", file);               // must match backend param name (IFormFile file)
//     modes.forEach((m) => fd.append("modes", m)); // must match backend param name (string[] modes)

//     const controller = new AbortController();
//     abortRef.current = controller;

//     try {
//       setIsUploading(true);

//       // IMPORTANT: do NOT set Content-Type manually (axios will add boundary)
//       const res = await axios.post("/api/file/generate", fd, {
//         signal: controller.signal,
//         withCredentials: true, // if needed
//         onUploadProgress: (e) => {
//           if (!e.total) return;
//           setProgress(Math.round((e.loaded * 100) / e.total));
//         },
//       });

//       // res.data could be: { jobId } or { pdfUrl } etc.
//       console.log("Generate result:", res.data);
//       return res.data;
//     } catch (err) {
//       if (err.name === "CanceledError") {
//         console.log("Upload canceled");
//       } else {
//         console.error(err);
//       }
//     } finally {
//       setIsUploading(false);
//     }
//   };

//   const onCancelUpload = () => {
//     abortRef.current?.abort();
//   };

//   return (
//     <div style={{ maxWidth: 520 }}>
//       <h2>Upload → Select modes → Generate</h2>

//       <input type="file" accept=".pdf" onChange={onPickFile} />

//       {file && (
//         <div style={{ marginTop: 12 }}>
//           <div>
//             <strong>{file.name}</strong> — {Math.round(file.size / 1024 / 1024)} MB
//           </div>

//           <button onClick={onRemoveFile} disabled={isUploading}>
//             Remove file
//           </button>
//         </div>
//       )}

//       <div style={{ marginTop: 12 }}>
//         <p>Modes:</p>
//         <label>
//           <input
//             type="checkbox"
//             checked={modes.includes("ERD")}
//             onChange={() => toggleMode("ERD")}
//             disabled={isUploading}
//           />
//           ERD
//         </label>
//         <br />
//         <label>
//           <input
//             type="checkbox"
//             checked={modes.includes("YAML")}
//             onChange={() => toggleMode("YAML")}
//             disabled={isUploading}
//           />
//           YAML
//         </label>
//       </div>

//       <div style={{ marginTop: 12 }}>
//         <button onClick={onGenerate} disabled={!file || isUploading}>
//           {isUploading ? "Uploading..." : "Generate"}
//         </button>

//         <button onClick={onCancelUpload} disabled={!isUploading}>
//           Cancel upload
//         </button>
//       </div>

//       <div style={{ marginTop: 12 }}>
//         Upload progress: {progress}%
//       </div>
//     </div>
//   );
// }