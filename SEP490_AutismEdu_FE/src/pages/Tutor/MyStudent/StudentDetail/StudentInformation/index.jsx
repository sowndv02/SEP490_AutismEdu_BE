import { Avatar, Box, Chip, Grid, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import CompleteTutoring from '../CompleteTutoring';
import { useEffect, useState } from 'react';
import { format } from 'date-fns';

function StudentInformation({ studentProfile, setStudentProfile }) {
    const [status, setStatus] = useState(1);
    const [assessments, setAssessments] = useState([])
    useEffect(() => {
        if (studentProfile) {
            setAssessments(studentProfile?.initialAssessmentResults.assessmentResults)
        }
    }, [studentProfile])

    useEffect(() => {
        if (status === 2) {
            setAssessments(studentProfile?.finalAssessmentResults.assessmentResults)
        } else {
            setAssessments(studentProfile?.initialAssessmentResults.assessmentResults)
        }
    }, [status])
    const formatDate = (date) => {
        if (!date) return "";
        const d = new Date(date);
        return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`
    }

    const getPointColor = (point) => {
        if (point < 1.5) return "#ff6666";
        if (point < 2) return "#ffa500";
        if (point < 2.5) return "#ffd700";
        if (point < 3.5) return "#9acd32";
        if (point < 4) return "#32cd32";
        return "#1e90ff";
    };

    return (
        <Stack sx={{ width: "100%", p: 5 }} direction='row' justifyContent="center">
            <Stack direction='row' sx={{
                width: "100%",
                margin: "auto",
                mt: "20px",
                gap: 2
            }}>
                <Box sx={{
                    width: "70%",
                    padding: "20px",
                    backgroundColor: "#f9f9f9",
                    borderRadius: "10px",
                    boxShadow: "0 4px 10px rgba(0, 0, 0, 0.1)"
                }}>
                    {
                        studentProfile?.status === 0 && (
                            <Box my={3} sx={{ textAlign: "center" }}>
                                <Chip
                                    label="Tình trạng ban đầu"
                                    variant={status === 1 ? "filled" : "outlined"}
                                    onClick={() => setStatus(1)}
                                    sx={{
                                        cursor: "pointer",
                                        mr: 2,
                                        bgcolor: status === 1 ? "#b660ec" : "transparent",
                                        color: status === 1 ? "white" : "inherit",
                                        "&:hover": { bgcolor: "#d58ced", color: "white" }
                                    }}
                                />
                                <Chip
                                    label="Kết quả cuối cùng"
                                    variant={status === 2 ? "filled" : "outlined"}
                                    onClick={() => setStatus(2)}
                                    sx={{
                                        cursor: "pointer",
                                        bgcolor: status === 2 ? "#3c4ff4" : "transparent",
                                        color: status === 2 ? "white" : "inherit",
                                        "&:hover": { bgcolor: "#5a6af5", color: "white" }
                                    }}
                                />
                            </Box>
                        )
                    }
                    <Typography variant='h3' sx={{ textAlign: "center", fontWeight: "bold", mt: 2 }}>
                        {status === 1 ? "Tình trạng ban đầu" : "Kết quả cuối cùng"}
                    </Typography>
                    <Typography mt={2} sx={{
                        whiteSpace: "break-spaces", textAlign: "center", wordBreak: 'break-word',
                        overflowWrap: 'break-word'
                    }}>
                        {status === 1 ? studentProfile?.initialAssessmentResults.condition : studentProfile?.finalAssessmentResults.condition}
                    </Typography>
                    <Typography variant='h3' mt={5} sx={{ fontWeight: "bold", mb: 2 }}>
                        Bảng đánh giá
                    </Typography>
                    <TableContainer component={Paper} sx={{
                        mt: 2, borderRadius: "10px",
                        maxHeight: "500px",
                        overflow: "auto"
                    }}>
                        <Table stickyHeader sx={{ minWidth: 650 }} aria-label="simple table">
                            <TableHead>
                                <TableRow sx={{ backgroundColor: "#e0e0e0" }}>
                                    <TableCell sx={{ fontWeight: "bold", color: "#333" }}>Vấn đề</TableCell>
                                    <TableCell sx={{ fontWeight: "bold", color: "#333" }}>Đánh giá</TableCell>
                                    <TableCell align='right' sx={{ fontWeight: "bold", color: "#333" }}>Điểm</TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody sx={{ maxHeight: "50vh", overflow: "auto" }}>
                                {assessments.length !== 0 && assessments.map((assessment) => (
                                    <TableRow
                                        key={assessment.id}
                                        sx={{ '&:last-child td, &:last-child th': { border: 0 } }}
                                    >
                                        <TableCell>{assessment.question}</TableCell>
                                        <TableCell>{assessment.optionText}</TableCell>
                                        <TableCell
                                            align='right'
                                            sx={{ color: getPointColor(assessment.point), fontWeight: "bold" }}
                                        >
                                            {assessment.point}
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>
                </Box>
                <Box sx={{ width: "40%" }}>
                    <Box sx={{
                        background: `linear-gradient(to bottom, #c079ea, #4468f1)`,
                        transition: 'transform 0.3s ease, height 0.5s ease',
                        borderRadius: "16px",
                        maxHeight: "400px",
                        py: "40px",
                        px: "20px",
                        boxShadow: "0px 4px 20px rgba(0, 0, 0, 0.2)",
                        "&:hover": {
                            transform: "scale(1.03)",
                            boxShadow: "0px 8px 30px rgba(0, 0, 0, 0.3)"
                        },
                        mb: 2
                    }}>
                        <Avatar
                            alt='Khai Dao'
                            src={studentProfile?.imageUrlPath}
                            sx={{
                                height: "120px",
                                width: "120px",
                                fontSize: "36px",
                                bgcolor: "#f06292",
                                margin: "auto",
                                boxShadow: "0px 4px 10px rgba(0, 0, 0, 0.2)"
                            }}
                        />
                        <Grid container
                            sx={{
                                margin: "auto",
                                color: "white",
                                px: "24px",
                                fontSize: "16px",
                                mt: 2
                            }}
                            rowSpacing={1.5}
                            columnSpacing={2}
                        >
                            <Grid item xs={5} textAlign="right" sx={{ fontWeight: 600 }}>Họ và tên:</Grid>
                            <Grid item xs={7}>{studentProfile?.name}</Grid>
                            <Grid item xs={5} textAlign="right" sx={{ fontWeight: 600 }}>Giới tính:</Grid>
                            <Grid item xs={7}>{studentProfile?.isMale === true ? "Nam" : "Nữ"}</Grid>
                            <Grid item xs={5} textAlign="right" sx={{ fontWeight: 600 }}>Ngày sinh:</Grid>
                            <Grid item xs={7}>{formatDate(studentProfile?.birthDate)}</Grid>
                        </Grid>
                    </Box>
                    {
                        studentProfile?.status === 1 && (
                            <CompleteTutoring studentProfile={studentProfile} setStudentProfile={setStudentProfile} />
                        )
                    }
                </Box>
            </Stack>
        </Stack>
    )
}

export default StudentInformation
