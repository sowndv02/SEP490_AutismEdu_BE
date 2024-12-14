import { useEffect, useRef, useState } from 'react';
import {
    Box,
    Button,
    Stack,
    Typography,
    IconButton,
    Menu,
    MenuItem
} from '@mui/material';
import MoreHorizIcon from '@mui/icons-material/MoreHoriz';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import TestCreationModal from '../TestModal/TestCreationModal';
import TestQuestionCreationModal from '../TestModal/TestQuestionCreationModal';
import services from '~/plugins/services';
import LoadingComponent from '~/components/LoadingComponent';
import { enqueueSnackbar } from 'notistack';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';

function QuestionList({ selectedTest, setSelectedTest }) {
    const [loading, setLoading] = useState(false);
    const [anchorEl, setAnchorEl] = useState(null);
    const [selectedQuestion, setSelectedQuestion] = useState(null);
    const [openQuizCreation, setQuizCreation] = useState(false);
    const [questions, setQuestions] = useState([]);
    const testTitle = "Sample Test Title";
    const testDescription = "This is a brief description of the test.";

    const questionsData = [
        {
            id: 1,
            question: "What is the capital of France?",
            options: [
                { optionText: "Paris", point: 1 },
                { optionText: "London", point: 2 },
                { optionText: "Berlin", point: 3 },
                { optionText: "Madrid", point: 4 }
            ]
        },
        {
            id: 2,
            question: "What is 2 + 2?",
            options: [
                { optionText: "3", point: 1 },
                { optionText: "4", point: 2 },
                { optionText: "5", point: 3 },
                { optionText: "6", point: 4 }
            ]
        }
    ];


    useEffect(() => {
       
        handleGetQuestions();
    }, [selectedTest]);

    const handleGetQuestions = async () => {
        try {
            setLoading(true);
            await services.TestQuestionManagementAPI.getListTestQuestionByTestId(selectedTest?.id, (res) => {
                if (res?.result) {
                    setQuestions(res.result);
                }
            }, (error) => {
                console.log(error);
            })
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
        }
    };


    const handleMoreClick = (event, questionId) => {
        setAnchorEl(event.currentTarget);
        setSelectedQuestion(questionId);
    };

    const handleCloseMenu = () => {
        setAnchorEl(null);
        setSelectedQuestion(null);
    };

    const handleCloseQuizCreation = () => {
        setQuizCreation(false);
    };


    return (
        <Stack direction="column" sx={{ width: "90%", margin: "auto", gap: 2 }}>
            <Button onClick={() => setSelectedTest(null)} variant='outlined' sx={{ width: "15%" }} startIcon={<ArrowBackIcon />}>Quay lại</Button>
            <Typography variant="h4" sx={{ fontWeight: 'bold' }}>Bài kiểm tra: {selectedTest?.testName}</Typography>
            <Typography variant="body1" sx={{ mb: 2 }}>Nội dung: {selectedTest?.testDescription}</Typography>

            <Button variant="contained" color="primary" sx={{ alignSelf: 'flex-start', mb: 3 }} onClick={() => setQuizCreation(true)}>
                Tạo câu hỏi
            </Button>

            {questions.map((question, index) => (
                <Box
                    key={index}
                    sx={{
                        p: 2,
                        boxShadow: 2,
                        borderRadius: 1,
                        bgcolor: 'white',
                        position: 'relative'
                    }}
                >
                    <Typography variant="h6" sx={{ mb: 1 }}>{index + 1}. {question.question}</Typography>

                    <Stack direction="column" spacing={1}>
                        <Typography variant='subtitle1'>Câu trả lời:</Typography>
                        {question.assessmentOptions.map((option, index) => (
                            <Typography key={index} variant="subtitle2" sx={{ pl: 2 }}>
                                {index + 1}, {option.optionText} - {`(${option.point} Điểm)`}
                            </Typography>
                        ))}
                    </Stack>


                    <IconButton
                        aria-label="more"
                        onClick={(e) => handleMoreClick(e, question.id)}
                        sx={{ position: 'absolute', top: 8, right: 8 }}
                    >
                        <MoreHorizIcon />
                    </IconButton>
                    <Menu anchorEl={anchorEl} open={Boolean(anchorEl) && selectedQuestion === question.id} onClose={handleCloseMenu}>
                        <MenuItem onClick={() => { handleCloseMenu }}>
                            <EditIcon fontSize="small" color='primary' sx={{ mr: 1 }} />
                            Chỉnh sửa
                        </MenuItem>
                        <MenuItem onClick={() => { handleCloseMenu }}>
                            <DeleteIcon fontSize="small" color='error' sx={{ mr: 1 }} />
                            Xoá
                        </MenuItem>
                    </Menu>
                </Box>
            ))}
            <LoadingComponent open={loading} setOpen={setLoading} />
            {openQuizCreation && <TestQuestionCreationModal open={openQuizCreation} handleClose={handleCloseQuizCreation} testId={selectedTest?.id} setQuestions={setQuestions} />}
        </Stack>
    );
}

export default QuestionList;
