import React, { useState } from 'react';
import {
    Box,
    Button,
    Card,
    Grid,
    IconButton,
    TextField,
    Typography,
    MenuItem,
    Select,
    InputLabel,
    FormControl,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Divider
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import services from '~/plugins/services';
import LoadingComponent from '~/components/LoadingComponent';
import { enqueueSnackbar } from 'notistack';

function TestQuestionCreationModal({ open, handleClose, testId, setQuestions}) {
    const [loading, setLoading] = useState(false);
    const [question, setQuestion] = useState('');
    const [options, setOptions] = useState([{ optionText: '', point: 1 }, { optionText: '', point: 1 }, { optionText: '', point: 1 }, { optionText: '', point: 1 }]);

    const handleOptionChange = (index, value) => {
        const newOptions = [...options];
        newOptions[index].optionText = value.trim();
        setOptions(newOptions);
    };

    const handleScoreChange = (index, score) => {
        const newOptions = [...options];
        newOptions[index].point = score;
        setOptions(newOptions);
    };

    const handleDeleteOption = (index) => {
        const newOptions = options.filter((_, i) => i !== index);
        setOptions(newOptions);
    };

    const handleAddOption = () => {
        setOptions([...options, { optionText: '', point: 1 }]);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        const quizData = {
            testId,
            question,
            options
        };
        console.log(quizData);

        try {
            setLoading(true);
            await services.TestQuestionManagementAPI.createTestQuestion(quizData, (res) => {
                if (res?.result) {
                    setQuestions((prev)=>[...prev, res?.result]);
                    enqueueSnackbar("Tạo câu hỏi thành công!", { variant: 'success' });
                }
            }, (error) => {
                console.log(error);
                enqueueSnackbar(error.error[0], { variant: 'error' });
            });
        } catch (error) {
            console.log(error);
        } finally {
            setLoading(false);
            handleClose();
        }
    };

    return (
        <Dialog open={open} onClose={handleClose} fullWidth maxWidth="sm">
            <DialogTitle textAlign="center" variant='h5'>Create Quiz</DialogTitle>
            <Divider />
            <form onSubmit={handleSubmit}>

                <DialogContent sx={{ bgcolor: 'white', color: 'black' }}>
                    <TextField
                        label="Nhập câu hỏi ở đây"
                        value={question}
                        onChange={(e) => setQuestion(e.target.value)}
                        variant="outlined"
                        fullWidth
                        multiline
                        required
                        rows={2}
                        sx={{ my: 3 }}
                    />

                    <Grid container spacing={2}>
                        {options.map((option, index) => (
                            <Grid item xs={12} key={index}>
                                <Card
                                    sx={{
                                        display: 'flex',
                                        alignItems: 'center',
                                        p: 2,
                                        bgcolor: 'white',
                                        color: 'black',
                                        gap: 2
                                    }}
                                >
                                    <TextField
                                        fullWidth
                                        value={option.text}
                                        onChange={(e) => handleOptionChange(index, e.target.value)}
                                        label={`Nhập câu trả lời thứ ${index + 1}`}
                                        variant="outlined"
                                        required
                                    />

                                    <FormControl variant="outlined" sx={{ minWidth: 100 }}>
                                        <InputLabel>Điểm</InputLabel>
                                        <Select
                                            value={option.point}
                                            onChange={(e) => handleScoreChange(index, e.target.value)}
                                            label="Điểm"
                                        >
                                            <MenuItem value={1}>1</MenuItem>
                                            <MenuItem value={2}>2</MenuItem>
                                            <MenuItem value={3}>3</MenuItem>
                                            <MenuItem value={4}>4</MenuItem>
                                        </Select>
                                    </FormControl>

                                    <IconButton onClick={() => handleDeleteOption(index)} color="error">
                                        <DeleteIcon/>
                                    </IconButton>
                                </Card>
                            </Grid>
                        ))}
                    </Grid>

                    <Button
                        onClick={handleAddOption}
                        variant="contained"
                        sx={{
                            mt: 3,
                            backgroundColor: '#F3EBFF', 
                            color: '#8B47D8', 
                            border: '1px solid #8B47D8', 
                            fontSize: '16px',
                            borderRadius: '8px',
                            '&:hover': {
                                color: '#d2a6f5',
                                backgroundColor:'#F3EBFF'
                            },
                        }}
                    >
                        Thêm câu trả lời
                    </Button>

                </DialogContent>
                <Divider />

                <DialogActions>
                    <Button onClick={handleClose} variant='outlined' color="inherit">Huỷ</Button>
                    <Button type='submit' variant="contained" color="primary">
                        Tạo
                    </Button>
                </DialogActions>
            </form>
            <LoadingComponent open={loading} setOpen={setLoading} />

        </Dialog>
    );
}

export default TestQuestionCreationModal;
