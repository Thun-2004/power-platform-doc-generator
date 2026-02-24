
/**
 * 
 * @param {AxiosInstance} axiosInst 
 * @param {File} file 
 * @param {string[]} outputTypes 
 * @returns {Promise<any>}
 */

export default async function uploadFile(axiosInst, file, outputTypes) {
    const response = await axiosInst.post('/generate', {
        file,
        outputTypes
    });
    return response.data;
}

