import React, { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import {
    Box,
    Button,
    Stack,
    Typography,
    Radio,
    RadioGroup,
    FormControlLabel,
    Dialog,
    DialogActions,
    DialogContent,
    DialogContentText,
    DialogTitle,
    Divider,
} from '@mui/material';
import services from '~/plugins/services';
import LoadingComponent from '~/components/LoadingComponent';
import { enqueueSnackbar } from 'notistack';
import { setUserInformation, userInfor } from '~/redux/features/userSlice';
import { useDispatch, useSelector } from 'react-redux';

function DoTest() {
    const userInfo = useSelector(userInfor);
    console.log(userInfo);
    const location = useLocation();
    const [test, setTest] = useState(location.state?.selectedTest ?? null);
    const [loading, setLoading] = useState(false);
    const [questions, setQuestions] = useState([]);
    const [testResults, setTestResults] = useState(() => {
        const savedResults = localStorage.getItem(`testResults-${userInfo?.id}-${test?.id}`);
        return savedResults ? JSON.parse(savedResults) : [];
    });
    const [openDialog, setOpenDialog] = useState(false);
    const [score, setScore] = useState(null);
    const [noAnswer, setNoAnswer] = useState(false);

    useEffect(() => {
        handleGetQuestions();
    }, [test]);

    const handleGetQuestions = async () => {
        try {
            setLoading(true);
            await services.TestQuestionManagementAPI.getListTestQuestionByTestId(test?.id, (res) => {
                if (res?.result) {
                    setQuestions(res.result);
                }
            }, (error) => {
                console.log(error);
            });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };

    const handleOptionChange = (questionId, optionId) => {
        const question = questions.find(q => q.id === questionId);
        const selectedOption = question?.assessmentOptions.find(option => option.id === optionId);

        if (selectedOption) {
            setTestResults((prevResults) => {
                const updatedResults = [...prevResults];
                const existingAnswerIndex = updatedResults.findIndex((result) => result.questionId === questionId);

                const answer = { questionId, optionId, point: selectedOption.point };

                if (existingAnswerIndex !== -1) {
                    updatedResults[existingAnswerIndex] = answer;
                } else {
                    updatedResults.push(answer);
                }

                localStorage.setItem(`testResults-${userInfo?.id}-${test?.id}`, JSON.stringify(updatedResults));
                return updatedResults;
            });
        }
    };

    const handleSubmit = async () => {
        const unansweredQuestions = questions.filter(
            (question) => !testResults.some((result) => result.questionId === question.id)
        );

        if (unansweredQuestions.length > 0) {
            if (unansweredQuestions.length === questions.length) {
                setNoAnswer(true);
            }
            setOpenDialog(true);
        } else {
            const totalScore = testResults.reduce((sum, result) => sum + result.point, 0);
            setScore(totalScore);

            try {
                setLoading(true);
                const newData = {
                    "testId": test?.id,
                    "totalPoint": totalScore,
                    "testResults": testResults.map((r) => ({ questionId: r.questionId, optionId: r.optionId }))
                };
                await services.TestResultManagementAPI.createSubmitTest(newData, (res) => {
                    enqueueSnackbar("Nộp bài thành công!", { variant: 'success' });
                }, (error) => {
                    console.log(error);
                });
                localStorage.removeItem(`testResults-${userInfo?.id}-${test?.id}`);
            } catch (error) {
                console.log(error);
            } finally {
                setLoading(false);
            }
        }
    };

    const handleConfirmSubmit = async () => {
        setOpenDialog(false);
        const totalScore = testResults.reduce((sum, result) => sum + result.point, 0);
        setScore(totalScore);

        try {
            setLoading(true);
            const newData = {
                "testId": test?.id,
                "totalPoint": totalScore,
                "testResults": testResults.map((r) => ({ questionId: r.questionId, optionId: r.optionId }))
            };
            await services.TestResultManagementAPI.createSubmitTest(newData, (res) => {
                if (res?.result) {
                    enqueueSnackbar("Nộp bài thành công!", { variant: 'success' });
                }
            }, (error) => {
                console.log(error);
            });

            localStorage.removeItem(`testResults-${userInfo?.id}-${test?.id}`);
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };

    return (
        <Stack direction="column" sx={{ width: "80%", margin: "auto", gap: 2, py: 5 }}>
            <Typography variant="h4" sx={{ fontWeight: 'bold' }}>Bài kiểm tra: {test?.testName}</Typography>
            <Typography variant="body1" sx={{ mb: 2 }}>Nội dung: {test?.testDescription}</Typography>

            {questions.map((question, index) => (
                <Box
                    key={question.id}
                    sx={{
                        p: 2,
                        boxShadow: 2,
                        borderRadius: 1,
                        bgcolor: 'white',
                        position: 'relative'
                    }}
                >
                    <Typography variant="h6" sx={{ mb: 1 }}>{index + 1}. {question.question}</Typography>

                    <RadioGroup
                        value={
                            testResults.find((result) => result.questionId === question.id)?.optionId || ''
                        }
                        onChange={(e) => handleOptionChange(question.id, parseInt(e.target.value))}
                    >
                        {question.assessmentOptions.map((option, idx) => (
                            <FormControlLabel
                                key={option.id}
                                value={option.id}
                                control={<Radio />}
                                label={`${idx + 1}. ${option.optionText} (${option.point} Điểm)`}
                            />
                        ))}
                    </RadioGroup>
                </Box>
            ))}

            <Box display={'flex'} justifyContent={'right'}>
                <Button variant="contained" color="primary" onClick={handleSubmit} disabled={score}>
                    Nộp bài
                </Button>
            </Box>

            <LoadingComponent open={loading} setOpen={setLoading} />

            <Dialog
                open={openDialog}
                onClose={() => setOpenDialog(false)}
            >
                <DialogTitle variant='h5' textAlign={'center'}>Xác nhận nộp bài</DialogTitle>
                <Divider />
                <DialogContent sx={{ height: "100px", display: 'flex', alignItems: 'center' }}>
                    <DialogContentText>
                        Một số câu hỏi chưa được chọn câu trả lời. Bạn có chắc chắn muốn nộp bài không?
                    </DialogContentText>
                </DialogContent>
                <Divider />
                <DialogActions>
                    <Button onClick={() => { setOpenDialog(false); setNoAnswer(false); }} color="inherit" variant='outlined'>
                        Huỷ
                    </Button>
                    <Button onClick={handleConfirmSubmit} color="primary" variant='contained' disabled={noAnswer}>
                        Đồng ý
                    </Button>
                </DialogActions>
            </Dialog>

            {score && (
                <Box sx={{ mt: 3, textAlign: 'center' }}>
                    <Typography variant="h6" color={'green'}>Kết quả: {score} Điểm</Typography>
                </Box>
            )}
        </Stack>
    );
}

export default DoTest;
