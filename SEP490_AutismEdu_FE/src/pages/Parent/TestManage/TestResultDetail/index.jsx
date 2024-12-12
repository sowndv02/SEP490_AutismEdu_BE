import React, { useEffect, useState } from 'react';
import {
    Box,
    Typography,
    Stack,
    Divider,
    Radio,
    RadioGroup,
    FormControlLabel
} from '@mui/material';
import { useParams } from 'react-router-dom';
import services from '~/plugins/services';
import LoadingComponent from '~/components/LoadingComponent';

function TestResultDetail() {
    const { id } = useParams();
    const [result, setResult] = useState(null);
    // const result = {
    //     id: 3,
    //     testId: 15,
    //     testName: "Bai 3",
    //     testDescription: "Bai 3.3",
    //     totalPoint: 5,
    //     createdDate: "2024-11-09T02:33:58.092895",
    //     testQuestions: [
    //         {
    //             id: 4,
    //             question: "Doan con vat",
    //             createdDate: "2024-11-09T02:32:49.116774",
    //             assessmentOptions: [
    //                 { id: 119, optionText: "Con chim", point: 1 },
    //                 { id: 120, optionText: "Con ho", point: 1 },
    //                 { id: 121, optionText: "Con cao", point: 2 },
    //                 { id: 122, optionText: "Con su tu", point: 3 }
    //             ]
    //         },
    //         {
    //             id: 5,
    //             question: "Doan ten xe",
    //             createdDate: "2024-11-09T02:33:37.578383",
    //             assessmentOptions: [
    //                 { id: 126, optionText: "Poscher", point: 4 },
    //                 { id: 123, optionText: "Toyota", point: 1 },
    //                 { id: 124, optionText: "Mez", point: 2 },
    //                 { id: 125, optionText: "Audi", point: 3 }
    //             ]
    //         }
    //     ],
    //     results: [
    //         { id: 4, testResultId: 3, questionId: 4, optionId: 119, createdDate: "2024-11-09T02:33:58.092893" },
    //         { id: 5, testResultId: 3, questionId: 5, optionId: 126, createdDate: "2024-11-09T02:33:58.092895" }
    //     ]
    // };
    const [loading, setLoading] = useState(false);
    useEffect(() => {
        handleGetTestResultDetailHistory();
    }, [id]);

    const handleGetTestResultDetailHistory = async () => {
        try {
            setLoading(true);
            await services.TestResultManagementAPI.getTestResultDetailHistory(id, (res) => {
                if (res?.result) {
                    setResult(res.result);
                }
            })
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };


    return (
        <Stack direction="column" sx={{ width: "80%", margin: "auto", gap: 2, py: 5 }}>
            <Typography variant="h4" sx={{ fontWeight: 'bold' }}>Bài kiểm tra: {result?.testName}</Typography>
            <Typography variant="body1" sx={{ mb: 2 }}>Nội dung: {result?.testDescription}</Typography>
            <Typography variant="h6" color={'green'}>Tổng điểm: {result?.totalPoint} Điểm</Typography>
            <Divider sx={{ my: 2 }} />

            {result?.testQuestions?.map((question, index) => {
                const selectedOption = result?.results?.find(res => res?.questionId === question?.id)?.optionId;

                return (
                    <Box
                        key={question?.id}
                        sx={{
                            p: 2,
                            boxShadow: 2,
                            borderRadius: 1,
                            bgcolor: 'white',
                            position: 'relative'
                        }}
                    >
                        <Typography variant="h6" sx={{ mb: 1 }}>{index + 1}. {question?.question}</Typography>

                        <RadioGroup value={selectedOption || ''}>
                            {question?.assessmentOptions?.map((option, idx) => (
                                <FormControlLabel
                                    key={option.id}
                                    value={option.id}
                                    control={<Radio />}
                                    label={`${idx + 1}. ${option?.optionText} (${option?.point} Điểm)`}
                                    disabled
                                />
                            ))}
                        </RadioGroup>
                    </Box>
                );
            })}
            <LoadingComponent open={loading} setOpen={setLoading} />
        </Stack>
    );
}

export default TestResultDetail;
