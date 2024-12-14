import { Box, Typography } from '@mui/material';
import Paper from '@mui/material/Paper';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import { useEffect, useState } from 'react';
import services from '~/plugins/services';

function AssessmentGuildClient() {
    const [assessments, setAssessments] = useState([]);
    const [scoreRanges, setScoreRanges] = useState([]);

    useEffect(() => {
        const getAssessments = async () => {
            try {
                await services.AssessmentManagementAPI.listAssessment(
                    (res) => {
                        setAssessments(res.result.questions);
                        setScoreRanges(res.result.scoreRanges);
                    },
                    (err) => {
                        console.log(err);
                    }
                );
            } catch (error) {
                console.log(error);
            }
        };
        getAssessments();
    }, []);

    return (
        <Box
            px="200px"
            pt={2}
            mt="65px"
            overflow="auto"
            sx={{
                backgroundColor: '#f5f5f5'
            }}
        >
            <Typography variant="h4" sx={{ color: '#3c4ff4', fontWeight: 'bold' }}>
                Bài Test đánh giá trẻ tự kỷ (CARS)
            </Typography>
            <Typography variant="h5" mt={3} sx={{ color: '#b660ec', fontWeight: 'bold' }}>
                Hướng dẫn thực hiện bài Test, chấm điểm và đánh giá
            </Typography>
            <Typography sx={{ fontSize: '18px', color: '#333', mt: 2, lineHeight: 1.8 }}>
                Thang đánh giá gồm 15 vấn đề, mỗi mục có 4 mức độ. Người đánh giá quan sát trẻ, đánh giá các hành vi
                tương ứng với mỗi mức độ của mục đó.
            </Typography>
            <ul style={{ fontSize: '18px', color: '#555', lineHeight: 1.8 }}>
                <li>Bất thường nặng: 1 điểm</li>
                <li>Bất thường trung bình: 2 điểm</li>
                <li>Bất thường nhẹ: 3 điểm</li>
                <li>Bình thường: 4 điểm</li>
                <li>
                    Lưu ý: Bạn có thể dùng các mức thang đánh giá 1.5, 2.5 hoặc 3.5 nếu đứa trẻ đó ở mức tương đối giữa các
                    tiêu chí trên.
                </li>
            </ul>
            <Typography variant="h5" mt={3} sx={{ color: '#b660ec', fontWeight: 'bold' }}>
                Thực hiện bài đánh giá tự kỷ CARS
            </Typography>
            {assessments &&
                assessments.length !== 0 &&
                assessments.map((a, index) => (
                    <>
                        <Typography
                            variant="h6"
                            mt={4}
                            sx={{
                                color: '#3c4ff4',
                                fontWeight: 'bold',
                                borderBottom: '2px solid #b660ec',
                                pb: 1,
                            }}
                        >
                            VẤN ĐỀ {index + 1}: {a.question}
                        </Typography>
                        <TableContainer
                            component={Paper}
                            sx={{
                                mt: 2,
                                borderRadius: '8px',
                                boxShadow: '0 4px 12px rgba(0, 0, 0, 0.1)',
                            }}
                        >
                            <Table sx={{ minWidth: 650, border: '1px solid #ddd' }}>
                                <TableHead>
                                    <TableRow sx={{ backgroundColor: '#b660ec' }}>
                                        <TableCell sx={{ fontSize: '18px', fontWeight: 'bold', color: '#fff' }}>
                                            Điểm
                                        </TableCell>
                                        <TableCell sx={{ fontSize: '18px', fontWeight: 'bold', color: '#fff' }}>
                                            Đánh giá
                                        </TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {a.assessmentOptions.map((option) => (
                                        <TableRow
                                            key={option.id}
                                            sx={{
                                                '&:nth-of-type(odd)': { backgroundColor: '#f9f9f9' },
                                                '&:hover': { backgroundColor: '#f0f8ff' },
                                            }}
                                        >
                                            <TableCell sx={{ fontSize: '18px' }}>{option.point}</TableCell>
                                            <TableCell sx={{ fontSize: '18px' }}>{option.optionText}</TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    </>
                ))}
            <Typography variant="h4" mt={4} sx={{ color: '#3c4ff4', fontWeight: 'bold' }}>
                Kết quả đánh giá tự kỷ
            </Typography>
            <Typography sx={{ fontSize: '20px', mt: 2, color: '#333' }}>
                Tổng điểm được tính bằng cách cộng số điểm mỗi câu:
            </Typography>
            <ul>
                {scoreRanges &&
                    scoreRanges.length !== 0 &&
                    scoreRanges.map((s) => (
                        <li key={s.id} style={{ fontSize: '20px', color: '#555', marginTop: '10px' }}>
                            Nếu tổng điểm từ {s.minScore} - {s.maxScore}: {s.description}
                        </li>
                    ))}
            </ul>
        </Box>
    );
}

export default AssessmentGuildClient;
